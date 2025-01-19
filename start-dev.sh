#!/bin/bash

# Start the backend
echo "Starting ASP.NET Core backend..."
cd TrendyolProductAPI
dotnet run &
BACKEND_PID=$!

# Wait a bit for backend to initialize
sleep 5

# Start the frontend
echo "Starting React frontend..."
cd ../frontend
npm run dev &
FRONTEND_PID=$!

# Handle script termination
trap 'kill $BACKEND_PID $FRONTEND_PID' SIGINT SIGTERM

# Keep script running
wait 