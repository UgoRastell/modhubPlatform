#!/bin/bash
set -e

echo "Health check for ModsGamingPlatform infrastructure"
echo "------------------------------------------------"

# Check MongoDB
echo -n "MongoDB: "
if mongosh --host mongodb:27017 -u root -p example --eval "db.adminCommand('ping')" | grep -q '"ok" : 1'; then
  echo "[OK]"
else
  echo "[FAILED]"
  exit 1
fi

# Check RabbitMQ
echo -n "RabbitMQ: "
if rabbitmqctl -n rabbit@rabbitmq status | grep -q "RabbitMQ on"; then
  echo "[OK]"
else
  echo "[FAILED]"
  exit 1
fi

echo "All services are running!"
exit 0
