---
marp: true
theme: default
paginate: true
header: 'Page UI Project Overview'
footer: 'Creativity Without Limits'
backgroundColor: #f5f5f5
---

# Page UI: Flutter & Clean Architecture
### "Creativity Without Limits"
**A Comprehensive Project Walkthrough**

---

# 🏗 Architecture: Clean Architecture
- **Separation of Concerns:** Independent of UI, Database, and Frameworks.
- **Testable:** Logic is isolated in the Domain layer.
- **Scalable:** Easy to add new features without breaking existing ones.

---

# 🛠 The Three Layers

1. **Presentation:** Cubits (Bloc), Views, and Reusable Widgets.
2. **Domain:** Entities, Use Cases, and Repository Interfaces (The "Heart").
3. **Data:** Repository Implementations, Models (DTOs), and Data Sources (GraphQL/REST).

---

# 📁 Project Structure

- `lib/config`: Routes & Themes.
- `lib/core`: Shared logic (Network, Database, Helpers).
- `lib/features`: Domain-driven feature modules (Auth, Chat).

---

# ⚙️ Core Configuration: Routing
- **GoRouter:** Declarative navigation.
- **Auth Guards:** Automatic redirection based on login state.
- **Transitions:** Custom slide and fade animations for a premium feel.

---

# 💉 Core Configuration: Dependency Injection
- **GetIt:** Fast service locator.
- **Setup:** Centralized in `setUpServiceLocator()`.
- **Scope:** Manages Singletons for services and Factories for Cubits.

---

# 🌐 GraphQL Integration
- **GraphQLConfig:** Centralized client management.
- **Links:**
    - `AuthLink`: Secure header injection.
    - `WebSocketLink`: Real-time subscriptions for Chat.
    - `ErrorLink`: The "Magic" behind automatic token refresh.

---

# 🔄 Token Refresh Flow
1. **Request fails** with 401/Unauthorized.
2. **ErrorLink intercepts** the failure.
3. **RefreshToken Mutation** is called in the background.
4. **New tokens** are persisted to Secure Storage.
5. **Original request is retried** seamlessly.

---

# 🚀 Main Features: Auth & Chat
- **Authentication:** Secure login/register with JWT persistence.
- **Real-time Chat:** 
    - Instant messaging via Subscriptions.
    - History fetching.
    - Robust file upload service for attachments.

---

# 📦 The Tech Stack
- **State Management:** `flutter_bloc`
- **Network:** `graphql_flutter` & `dio`
- **Navigation:** `go_router`
- **Functional:** `dartz` (Either/Option)
- **Local Storage:** `flutter_secure_storage`

---

# 🏁 Conclusion & Key Takeaways
- **Clean Architecture** ensures long-term maintainability.
- **Robust Networking** with automated token management.
- **Modular Features** allow for parallel development.

---

# Q&A
**Thank You!**
