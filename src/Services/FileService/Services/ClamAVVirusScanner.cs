using FileService.Models;
using FileService.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Implementation of IVirusScanner using ClamAV antivirus engine
    /// </summary>
    public class ClamAVVirusScanner : IVirusScanner
    {
        private readonly VirusScanSettings _scanSettings;
        private readonly ILogger<ClamAVVirusScanner> _logger;

        public ClamAVVirusScanner(
            VirusScanSettings scanSettings,
            ILogger<ClamAVVirusScanner> logger)
        {
            _scanSettings = scanSettings;
            _logger = logger;
        }

        /// <summary>
        /// Scans a file for viruses using ClamAV
        /// </summary>
        public async Task<ScanResult> ScanFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("Starting virus scan for file: {fileName}", fileName);

                var scanResult = new ScanResult
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    ScanDate = DateTime.UtcNow,
                    Scanner = "ClamAV",
                    Status = ScanStatus.Pending,
                    LastUpdated = DateTime.UtcNow
                };

                // Check if scan service is enabled
                if (!_scanSettings.Enabled)
                {
                    _logger.LogWarning("Virus scanning is disabled. File {fileName} marked as clean without scanning", fileName);
                    scanResult.Status = ScanStatus.Clean;
                    scanResult.StatusMessage = "Scanning disabled - file automatically approved";
                    scanResult.LastUpdated = DateTime.UtcNow;
                    return scanResult;
                }

                // Reset file stream position
                if (fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                }

                // Connect to ClamAV daemon
                using (var client = new TcpClient())
                {
                    // Use the configured timeout
                    var connectionTask = client.ConnectAsync(_scanSettings.ServerHost, _scanSettings.ServerPort);
                    
                    // Wait for connection with timeout
                    if (await Task.WhenAny(connectionTask, Task.Delay(_scanSettings.TimeoutMilliseconds)) != connectionTask)
                    {
                        _logger.LogError("Timeout connecting to ClamAV at {host}:{port}", 
                            _scanSettings.ServerHost, _scanSettings.ServerPort);
                        
                        scanResult.Status = ScanStatus.Error;
                        scanResult.StatusMessage = "Connection timeout to virus scanner";
                        scanResult.LastUpdated = DateTime.UtcNow;
                        return scanResult;
                    }
                    
                    // Complete connection
                    await connectionTask;

                    // Check if file size exceeds the scanner limit
                    if (fileStream.Length > _scanSettings.MaxFileSizeBytes)
                    {
                        _logger.LogWarning("File {fileName} size ({size} bytes) exceeds virus scanner limit ({maxSize} bytes)",
                            fileName, fileStream.Length, _scanSettings.MaxFileSizeBytes);
                        
                        scanResult.Status = ScanStatus.Error;
                        scanResult.StatusMessage = $"File size exceeds scanner limit ({fileStream.Length} bytes)";
                        scanResult.LastUpdated = DateTime.UtcNow;
                        return scanResult;
                    }
                    
                    using (var networkStream = client.GetStream())
                    {
                        // Send INSTREAM command to ClamAV
                        var instream = Encoding.ASCII.GetBytes("zINSTREAM\0");
                        await networkStream.WriteAsync(instream, 0, instream.Length);

                        // Read file in chunks and send to ClamAV
                        byte[] buffer = new byte[_scanSettings.ChunkSizeBytes];
                        int bytesRead;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            // Send chunk size as 4-byte network-order integer
                            byte[] sizeBytes = BitConverter.GetBytes(bytesRead);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(sizeBytes);
                            await networkStream.WriteAsync(sizeBytes, 0, sizeBytes.Length);

                            // Send chunk data
                            await networkStream.WriteAsync(buffer, 0, bytesRead);
                        }

                        // Send zero-length chunk to signal end of stream
                        byte[] zeroBytes = new byte[] { 0, 0, 0, 0 };
                        await networkStream.WriteAsync(zeroBytes, 0, zeroBytes.Length);

                        // Read response from ClamAV
                        using (var reader = new StreamReader(networkStream, Encoding.ASCII, leaveOpen: true))
                        {
                            // Set timeout for reading
                            var readTask = reader.ReadLineAsync();
                            string response = null;
                            
                            if (await Task.WhenAny(readTask, Task.Delay(_scanSettings.TimeoutMilliseconds)) == readTask)
                            {
                                response = await readTask;
                            }
                            else
                            {
                                _logger.LogError("Timeout waiting for scan response from ClamAV");
                                
                                scanResult.Status = ScanStatus.Error;
                                scanResult.StatusMessage = "Timeout waiting for scan response";
                                scanResult.LastUpdated = DateTime.UtcNow;
                                return scanResult;
                            }

                            _logger.LogInformation("ClamAV scan response for {fileName}: {response}", fileName, response);
                            
                            // Process the response
                            scanResult.LastUpdated = DateTime.UtcNow;

                            if (response == null)
                            {
                                scanResult.Status = ScanStatus.Error;
                                scanResult.StatusMessage = "Empty response from scanner";
                            }
                            else if (response.EndsWith("OK"))
                            {
                                scanResult.Status = ScanStatus.Clean;
                                scanResult.StatusMessage = "No threats detected";
                            }
                            else if (response.Contains("FOUND"))
                            {
                                scanResult.Status = ScanStatus.Infected;
                                scanResult.StatusMessage = response;
                                
                                // Extract the threat name from response (format: "stream: ThreatName FOUND")
                                int foundIndex = response.IndexOf("FOUND");
                                if (foundIndex > 0)
                                {
                                    string threatPart = response.Substring(0, foundIndex).Trim();
                                    int colonIndex = threatPart.IndexOf(':');
                                    if (colonIndex >= 0 && colonIndex < threatPart.Length - 1)
                                    {
                                        scanResult.ThreatName = threatPart.Substring(colonIndex + 1).Trim();
                                    }
                                }
                            }
                            else if (response.Contains("ERROR"))
                            {
                                scanResult.Status = ScanStatus.Error;
                                scanResult.StatusMessage = response;
                            }
                            else
                            {
                                scanResult.Status = ScanStatus.Error;
                                scanResult.StatusMessage = "Unknown response: " + response;
                            }
                        }
                    }
                }
                
                _logger.LogInformation("Virus scan completed for {fileName} with status {status}: {message}", 
                    fileName, scanResult.Status, scanResult.StatusMessage);
                
                return scanResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file {fileName} for viruses", fileName);
                
                return new ScanResult
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    ScanDate = DateTime.UtcNow,
                    Scanner = "ClamAV",
                    Status = ScanStatus.Error,
                    StatusMessage = $"Scan error: {ex.Message}",
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Validates if the virus scanner is operational
        /// </summary>
        public async Task<bool> IsOperationalAsync()
        {
            try
            {
                if (!_scanSettings.Enabled)
                {
                    _logger.LogInformation("Virus scanner check skipped - scanning is disabled");
                    return true;
                }

                using (var client = new TcpClient())
                {
                    // Use the configured timeout for connection
                    var connectionTask = client.ConnectAsync(_scanSettings.ServerHost, _scanSettings.ServerPort);
                    
                    if (await Task.WhenAny(connectionTask, Task.Delay(_scanSettings.TimeoutMilliseconds)) != connectionTask)
                    {
                        _logger.LogError("ClamAV connection timeout during operational check");
                        return false;
                    }

                    // Complete connection
                    await connectionTask;
                    
                    using (var stream = client.GetStream())
                    {
                        // Send PING command to check if daemon is responsive
                        byte[] pingCmd = Encoding.ASCII.GetBytes("zPING\0");
                        await stream.WriteAsync(pingCmd, 0, pingCmd.Length);
                        
                        // Read response
                        using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
                        {
                            var readTask = reader.ReadLineAsync();
                            string response = null;
                            
                            if (await Task.WhenAny(readTask, Task.Delay(_scanSettings.TimeoutMilliseconds)) == readTask)
                            {
                                response = await readTask;
                            }
                            else
                            {
                                _logger.LogError("Timeout waiting for PING response from ClamAV");
                                return false;
                            }

                            if (response != null && response.Contains("PONG"))
                            {
                                _logger.LogInformation("ClamAV is operational");
                                return true;
                            }
                            else
                            {
                                _logger.LogError("ClamAV returned unexpected response to PING: {response}", response ?? "null");
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if virus scanner is operational");
                return false;
            }
        }

        /// <summary>
        /// Gets version information from the virus scanner
        /// </summary>
        public async Task<string> GetVersionAsync()
        {
            try
            {
                if (!_scanSettings.Enabled)
                {
                    return "Virus scanning disabled";
                }

                using (var client = new TcpClient())
                {
                    // Use the configured timeout
                    var connectionTask = client.ConnectAsync(_scanSettings.ServerHost, _scanSettings.ServerPort);
                    
                    if (await Task.WhenAny(connectionTask, Task.Delay(_scanSettings.TimeoutMilliseconds)) != connectionTask)
                    {
                        return "Connection timeout";
                    }

                    // Complete connection
                    await connectionTask;
                    
                    using (var stream = client.GetStream())
                    {
                        // Send VERSION command
                        byte[] versionCmd = Encoding.ASCII.GetBytes("zVERSION\0");
                        await stream.WriteAsync(versionCmd, 0, versionCmd.Length);
                        
                        // Read response
                        using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
                        {
                            var readTask = reader.ReadLineAsync();
                            
                            if (await Task.WhenAny(readTask, Task.Delay(_scanSettings.TimeoutMilliseconds)) == readTask)
                            {
                                string response = await readTask;
                                return response ?? "Empty response";
                            }
                            else
                            {
                                return "Timeout waiting for version";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virus scanner version");
                return $"Error: {ex.Message}";
            }
        }
    }
}
