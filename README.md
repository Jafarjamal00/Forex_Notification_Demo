# Backend Task — Real-Time Forex Notifications

## Overview
This project delivers a real-time Forex price notification system for a multi-user environment. Users subscribe to Forex symbols and receive live price updates every **500ms**.  
The system uses **SignalR** for real-time communication and **CQRS with MediatR** to separate reads and writes. All activity is authenticated, logged, and persisted to PostgreSQL.

## Goals
- Broadcast Forex prices using **SignalR**
- Consume WebSocket data from **Finnhub API**
- Push price updates via SignalR groups (per-symbol subscription model)
- Secure access using **JWT Bearer Authentication**
- Log connection, subscription, and broadcast activity
- Persist subscriptions and tick data into **PostgreSQL**

## Architecture & Tools
- **.NET 8 Web API + SignalR**
- **MediatR (CQRS – Commands & Queries)**
- **EF Core + PostgreSQL (PgAdmin)**
- **JWT Authentication & Authorization**
- **Serilog** structured logging
- **BackgroundService** workers for:
  - WebSocket ingestion (Finnhub)
  - Broadcasting price updates every 500ms

## Data Model
**Tables**
1. `price_tick` — `id, symbol, price, bid, ask, ts`
2. `subscription_audit` — `id, user_id, symbol, action, at`

## Implementation Steps
1. Implement `ForexHub` with SignalR groups (one per symbol).  
2. Secure hub connections using JWT Bearer authentication.  
3. Create Commands/Queries with MediatR for subscriptions and queries.  
4. Configure EF Core migrations and PostgreSQL schema.  
5. Build `FinnhubIngestService` to read live WebSocket data and update cache.  
6. Build `BroadcastService` to push cached prices every 500ms to SignalR groups.  
7. Enable structured logging using Serilog.  
8. Test subscriptions and verify real-time data delivery.

## Acceptance Criteria
- ✓ Only authorized users can connect and subscribe  
- ✓ Unauthorized users are rejected  
- ✓ Price updates delivered every 500ms  
- ✓ Subscription actions logged in database  
- ✓ Recent ticks retrievable via Query handlers  
- ✓ Logs capture connections, errors, and broadcasts  

## External API
Example configuration:
```json
"Finnhub": {
  "ApiKey": "d1ehg3pr01qjssrk5gkgd1ehg3pr01qjssrk5gl0"
}
