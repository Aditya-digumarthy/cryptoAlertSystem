# cryptoAlertSystem
“Real-time crypto price alerts with SignalR, MediatR (CQRS), and PostgreSQL.”


## 🚀 Real-Time Crypto Price Alert System

This project is a **.NET 8 Web API** that delivers real-time cryptocurrency price updates using **SignalR** and **MediatR (CQRS pattern)**.
It consumes live market data from a public WebSocket API (e.g., Binance) and broadcasts updates every **500ms** to subscribed users via SignalR groups.
The application is secured with **JWT Bearer Authentication**, uses **EF Core with PostgreSQL** for persistence, and logs activities using **Serilog**.
Users can subscribe/unsubscribe to trading pairs, while subscription audits and price ticks are stored in PostgreSQL.
Designed with clean architecture principles, background services handle WebSocket ingestion and real-time broadcasting efficiently.
