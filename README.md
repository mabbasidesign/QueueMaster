# QueueMaster

> Enterprise-grade cloud-native microservices platform built with .NET 8 and Docker, demonstrating async messaging with Azure Service Bus, event-driven architecture, and scalable API design.

## 🎯 Overview

QueueMaster is a modern microservices platform showcasing:
- **Async Communication** - Azure Service Bus (Queues & Topics)
- **Minimal APIs** - Clean, performant .NET 8 endpoints
- **Cloud-Native** - Designed for Azure Container Apps deployment
- **Event-Driven** - Decoupled services with pub/sub patterns
- **Production-Ready** - Health checks, logging, error handling

## 🏗️ Architecture

```
┌─────────────────────────────────────┐
│      Order Service (REST API)       │
│  - Create, Read, Update, Delete     │
│  - Publishes to Service Bus Queue   │
└────────────┬────────────────────────┘
             │
             ▼
      ┌──────────────────┐
      │ Azure Service    │
      │      Bus         │
      │  - Queue: orders │
      │  - Topic: events │
      └──────────┬───────┘
             │
             ▼
┌─────────────────────────────────────┐
│   Payment Service (Processor)       │
│  - Listens to queue messages        │
│  - Processes asynchronously         │
│  - Publishes to topic               │
└─────────────────────────────────────┘
```

## 📦 Services

### Order Service
**Minimal Web API** serving order operations
- `POST /api/v1/orders` - Create order
- `GET /api/v1/orders` - List all orders
- `GET /api/v1/orders/{id}` - Get order details
- `PUT /api/v1/orders/{id}` - Update order
- `DELETE /api/v1/orders/{id}` - Delete order

**Features:**
- ✅ Swagger/OpenAPI documentation
- ✅ Async request handling
- ✅ Error handling

### Payment Service
**Background message processor**
- Consumes order messages from Service Bus
- Processes payments asynchronously
- Publishes completion events
- (In development)

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker & Docker Compose (optional)
- Git

### Local Development

```bash
# Clone repository
git clone https://github.com/yourusername/QueueMaster.git
cd QueueMaster

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run Order Service
cd src/OrderService
dotnet run

# In another terminal, run Payment Service
cd src/PaymentService
dotnet run
```

### API Testing

```bash
# Create an order
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"John Doe"}'

# Get all orders
curl http://localhost:5000/api/v1/orders

# Access Swagger UI
# Order Service: http://localhost:5000/swagger
# Payment Service: http://localhost:5001/swagger
```

## 📁 Project Structure

```
QueueMaster/
├── src/
│   ├── OrderService/
│   │   ├── Program.cs          # Minimal API endpoints
│   │   ├── OrderService.csproj # Project file
│   │   └── Properties/         # Launch settings
│   │
│   └── PaymentService/
│       ├── Program.cs          # Service implementation
│       └── PaymentService.csproj
│
├── .github/
│   └── workflows/              # CI/CD pipelines
│
├── documentation/              # Architecture & guides
├── .gitignore                  # Git ignore rules
├── EventFlow.sln              # Solution file
└── README.md                  # This file
```

## 🛠️ Technology Stack

| Component | Technology |
|-----------|-----------|
| **Runtime** | .NET 8.0 |
| **API Framework** | Minimal APIs |
| **Async Messaging** | Azure Service Bus |
| **Database** | Azure SQL (upcoming) |
| **Containerization** | Docker |
| **Orchestration** | Azure Container Apps |
| **IaC** | Bicep |
| **CI/CD** | GitHub Actions |

## 🔄 Communication Flow

**Order Creation:**
1. Client sends POST request to Order Service
2. Order is validated and stored
3. Order event published to Service Bus Queue
4. Payment Service consumes message asynchronously
5. Payment processing begins

**Payment Completion:**
1. Payment Service processes payment
2. Publishes PaymentCompleted event to Topic
3. Subscribers (e.g., notification service) consume event
4. Order status updated

## ✨ Key Features

- 📡 **Async Messaging** - Decoupled microservices
- 🔄 **Retry Policies** - Automatic retry with backoff (coming soon)
- 💾 **Persistence** - SQL Server integration (coming soon)
- 📊 **Observability** - Application Insights (coming soon)
- 🔐 **Security** - Managed Identity, Key Vault (coming soon)
- 📈 **Scalability** - KEDA auto-scaling (coming soon)
- 🚢 **Deployment** - Bicep infrastructure templates (coming soon)

## 🌱 Roadmap

- [ ] Azure SQL database integration
- [ ] Service Bus retry policies & DLQ handling
- [ ] Application Insights telemetry
- [ ] API authentication & authorization
- [ ] Database migrations with EF Core
- [ ] Health checks endpoints
- [ ] Docker Compose local dev setup
- [ ] Bicep infrastructure templates
- [ ] GitHub Actions CI/CD pipeline
- [ ] Unit & integration tests
- [ ] Load testing scripts

## 📚 Documentation

- **[ARCHITECTURE.md](./documentation/ARCHITECTURE.md)** - System design details
- **[SETUP.md](./documentation/SETUP.md)** - Installation & configuration
- **[API.md](./documentation/API.md)** - API reference
- **[CONTRIBUTING.md](./CONTRIBUTING.md)** - Contribution guidelines

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific service tests
dotnet test src/OrderService.Tests
```

## 🐛 Troubleshooting

### Service won't start
```bash
# Clear build artifacts
dotnet clean

# Rebuild
dotnet build
```

### Port already in use
```powershell
# Find process using port 5000
netstat -ano | findstr :5000

# Kill process
taskkill /PID <PID> /F
```

## 📖 Learning Resources

This project demonstrates:
- ✅ Minimal APIs in .NET 8
- ✅ Async/await patterns
- ✅ Microservices architecture
- ✅ Message-driven design
- ✅ Cloud-native development
- ✅ OpenAPI/Swagger integration

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**EventFlow Development Team**
- Email: dev@eventflow.local
- GitHub: [yourusername](https://github.com/yourusername)

## 🙏 Acknowledgments

- [Microsoft .NET Documentation](https://docs.microsoft.com/dotnet/)
- [Azure Service Bus](https://azure.microsoft.com/services/service-bus/)
- [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

---

**Status:** 🟡 In Development

Latest Update: March 2, 2026
