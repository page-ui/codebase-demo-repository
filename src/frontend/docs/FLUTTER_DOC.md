# Page UI Project Documentation

## 1. Introduction
**Project Overview**
Page UI is a sophisticated, real-time communication platform designed to provide users with a seamless and interactive chat experience. The project focuses on high-performance web and mobile delivery, emphasizing a modern aesthetic combined with robust backend integration.

**Goals**
- To deliver a highly responsive and scalable real-time messaging system.
- To implement a secure, architecture-first codebase using Clean Architecture principles.
- To provide a visually immersive interface that enhances user engagement through motion and animations.

**Target Audience**
The platform is tailored for creative professionals, developers, and collaborative teams who require a high-fidelity communication tool that supports real-time data synchronization and complex media attachments.

---

## 2. User Guide
To ensure an optimal experience, users should follow these steps to navigate the Page UI ecosystem:

### Step 1: Authentication
- **Registration:** New users must create an account via the Register view.
- **Login:** Existing users can access their dashboard using their credentials.
- **Verification:** Secure email verification is required to activate full account features.

### Step 2: Onboarding
- Upon initial login, users are guided through an interactive "Train" or onboarding sequence that introduces the core interface elements and navigation patterns.

### Step 3: Engaging in Chat
- **Selecting a Conversation:** Users can choose from their chat history or initiate a new session.
- **Messaging:** Send real-time text messages and view instant updates via GraphQL subscriptions.
- **Attachments:** Upload and view media files directly within the chat interface.

### Step 4: External Integrations
- **URL Handling:** Clicking on external links within the chat or developers section will safely open a new browser tab using the platform's native web interoperability.
- **Embedded Content:** The system supports viewing specialized content via secure iframes when applicable.

---

## 3. System Design & Architecture
The Page UI system is built upon a modular and decoupled architecture to ensure long-term maintainability.

### Architectural Framework: Clean Architecture
The codebase is strictly organized into three distinct layers:
1. **Domain Layer:** Contains the core business logic (Use Cases) and data blueprints (Entities). It remains independent of any external libraries.
2. **Data Layer:** Handles data retrieval and persistence. It implements repository interfaces and manages communication with GraphQL and REST APIs.
3. **Presentation Layer:** Managed by the Bloc/Cubit pattern, this layer handles UI state and user interactions.

### Core Infrastructure (`lib/core`)
The `core` directory serves as the foundational backbone of the application, housing shared logic, utilities, and infrastructure-level configurations used across all features:

- **Constants:** Centralized management of application-wide values, including API endpoints, UI borders, and static strings.
- **Custom Widgets:** A library of reusable UI components such as animated backgrounds, custom buttons, and specialized loading indicators that maintain visual consistency.
- **Database & API:** 
    - **API:** Configuration of the `GraphQLClient` and `Dio` rest client, including interceptors for logging and authentication.
    - **Cache:** Secure storage implementations for sensitive user data like JWT tokens.
- **Errors & Failures:** A standardized error-handling system using `Failure` and `Exception` classes, integrated with `dartz` for functional error propagation.
- **Helpers:** Essential utility services including:
    - **Dependency Injection:** Centralized registration via `GetIt`.
    - **Logger:** Unified application logging for debugging and monitoring.
    - **Web Helpers:** Platform-specific logic for handling external links and browser interactions.
- **Network:** Infrastructure for monitoring network connectivity and status across mobile and web platforms.

### Component Structure
- **Frontend Framework:** Flutter (Dart)
- **State Management:** Flutter Bloc / Cubit
- **API Protocol:** GraphQL (Subscribed real-time updates) & REST (File handling)
- **Routing:** GoRouter
- **Storage:** Flutter Secure Storage

---

## 4. Implementation Overview
The development of Page UI utilized a "feature-first" approach, where each module (Auth, Chat, Intro) was developed as an isolated unit within the Clean Architecture boundaries.

### Core Implementation Details
- **Dependency Injection:** A centralized service locator (`GetIt`) manages the lifecycle of all services, ensuring efficient memory usage and easy testing.
- **Real-time Communication:** Implemented using `graphql_flutter`, leveraging web sockets for instantaneous message delivery.
- **Web Interoperability:** Utilizing the `web` and `js_interop` packages, the application supports advanced browser features.
    - **External Links:** A custom helper (`openExternalLink`) uses `web.window.open` to handle navigation to external sites in a new tab (`_blank`), ensuring the main application state remains intact.
    - **Iframe Management:** The architecture supports embedding external previews and training modules using HTML iframes, specifically managed to maintain responsiveness across different screen sizes.

### Advanced Chat Features
- **New Tab Navigation:** Integration with browser-native APIs allows users to explore external references without leaving their active chat session.
- **Embedded Previews:** The system uses iframe-based rendering for specific media types and external training tools, providing a unified experience within the Flutter web container.

### Key Dependencies
| Category | Packages |
| :--- | :--- |
| **Framework & UI** | `flutter`, `flutter_bloc`, `flutter_screenutil`, `google_fonts` |
| **Networking** | `graphql_flutter`, `dio`, `web` |
| **Navigation & DI** | `go_router`, `get_it` |
| **Persistence** | `flutter_secure_storage`, `shared_preferences` |
| **Logic & Utilities** | `dartz`, `logger`, `connectivity_plus` |
