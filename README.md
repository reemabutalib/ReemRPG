# ReemRPG

## Overview  
ReemRPG is a role-playing game (RPG) API built using **ASP.NET Core** and **Entity Framework Core**.  
This API allows players to create characters, embark on quests, manage inventory, and interact with various in-game entities.

## Features  
- Create, update, delete, and retrieve **Characters**, **Quests**, and **Items**  
- Manage character **inventory** and **quests**  
- Implement **DTOs**, **repositories**, and **services** for cleaner architecture  
- Uses **SQLite** as the database  
- RESTful API design with **Entity Framework Core** for data management  

---

## ⚙️ Technologies Used  
- **ASP.NET Core 7.0**  
- **Entity Framework Core**  
- **SQLite** (database)  
- **AutoMapper** (for mapping DTOs)  
- **Dependency Injection** for services and repositories  
- **Swagger (NSwag)** for API documentation  

---

##  Project Structure  
ReemRPG/ │── Controllers/ # API Controllers (Character, Item, Quest) │── DTOs/ # Data Transfer Objects (DTOs) │── Models/ # Entity models (Character, Item, Quest, Inventory) │── Repositories/ # Data repositories │ ├── Interfaces/ # Interfaces for repositories │── Services/ # Business logic layer │ ├── Interfaces/ # Interfaces for services │── Data/ # Application database context (ApplicationContext) │── Migrations/ # EF Core migrations │── appsettings.json # Configuration file (database connection) │── Program.cs # API entry point │── README.md # Project documentation


---

## 🔧 Setup & Installation  

### **1️ Clone the Repository**  
```sh
git clone https://github.com/your-username/ReemRPG.git
cd ReemRPG

Future Improvements
Implement authentication and authorization
Add unit and integration tests
Expand character skills and abilities
Improve error handling and logging