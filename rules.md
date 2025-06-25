# Project Development Rules and Guidelines

This document serves as the definitive guide for development standards, architectural decisions, operational procedures, and team collaboration frameworks for this project. Adherence to these rules is mandatory to ensure code quality, maintainability, scalability, and security.

## 1. Foundation and Strategic Context

### 1.1. Project Mission & Scope
- **Mission**: To build a high-quality, scalable, and maintainable [Product/Service Name] that solves [Problem Statement].
- **Scope**: The project encompasses the design, development, testing, and deployment of the core application, including its backend APIs, frontend interface, and underlying infrastructure.

### 1.2. Strategic Business Objectives
- Achieve market leadership through superior user experience and feature innovation.
- Ensure high availability (99.9% uptime) and performance for all users.
- Maintain compliance with industry standards (e.g., GDPR, CCPA).

### 1.3. Technology Stack Rationale
Our technology stack is chosen to optimize for developer productivity, performance, and scalability.
- **Runtime/Package Manager**: Bun (for TypeScript/JavaScript) for its speed and integrated tooling.
- **Linting/Formatting**: BiomeJS for its performance and all-in-one capabilities, replacing ESLint and Prettier.
- **Testing**: Vitest for unit/integration tests and Playwright for end-to-end tests.
- **Infrastructure**: Docker for containerization, Kubernetes for orchestration, and Terraform for IaC.
- **Frontend**: [e.g., React/Vite, Next.js, SvelteKit]
- **Backend**: [e.g., Node.js/Fastify, Go, Rust/Actix]
- **Database**: [e.g., PostgreSQL, MongoDB]
- **Cache**: Redis

### 1.4. Team Structure and Roles
- **Product Owner**: Defines features and prioritizes the backlog.
- **Tech Lead**: Oversees architectural decisions and technical standards.
- **Engineers**: Responsible for designing, building, and testing features.
- **QA/QE**: Responsible for quality assurance and test automation.

### 1.5. Development Methodology
We follow a Scrum-based agile methodology.
- **Sprints**: 2-week cycles.
- **Ceremonies**: Daily Standup, Sprint Planning, Sprint Review, Sprint Retrospective.
- **Task Management**: Jira/Linear/Shortcut [Link to board].

## 2. Development Environment Excellence Framework

### 2.1. Local Development Environment Setup
1.  Install `git`, `docker`, and `docker-compose`.
2.  Install `bun` globally: `curl -fsSL https://bun.sh/install | bash`.
3.  Clone the repository: `git clone [repository URL]`.
4.  Install dependencies: `bun install`.
5.  Set up environment variables (see below).
6.  Start services: `docker-compose up -d` and `bun dev`.

### 2.2. IDE and Editor Configuration
- **IDE**: VS Code is recommended.
- **Required Extensions**:
    - BiomeJS (`biomejs.biome`) for formatting and linting.
    - GitLens (`eamodio.gitlens`) for enhanced Git insights.
    - Docker (`ms-azuretools.vscode-docker`).
- **Settings**: Enable "Format on Save" to auto-format with Biome. This is configured in `.vscode/settings.json`.

### 2.3. Environment Variable Management
- A `.env.example` file is present in the root directory.
- Copy it to `.env`: `cp .env.example .env`.
- Populate `.env` with secrets and configuration values. **NEVER** commit the `.env` file to version control.
- Use a secrets management system like Doppler, Vault, or AWS Secrets Manager for production environments.

### 2.4. Database Setup
- The local database (e.g., PostgreSQL) runs in Docker.
- To initialize or reset the database, use the seeding script: `bun db:seed`.
- Migrations are handled by [e.g., Prisma, DrizzleORM]. Apply migrations with `bun db:migrate`.

### 2.5. Containerization (Docker)
- A `docker-compose.yml` file orchestrates local development containers (database, cache, etc.).
- The application `Dockerfile` uses multi-stage builds for optimized, secure production images.

### 2.6. Git Hooks and Pre-Commit Validation
- We use `simple-git-hooks` to enforce standards before committing.
- **Pre-commit hook**: Runs `bun lint` and `bun test:staged`.
- These hooks are configured in `package.json` and are installed automatically with `bun install`.

## 3. Code Quality and Standards Comprehensive Framework

### 3.1. Linting and Formatting
- **Tool**: BiomeJS (`biome.json`).
- **Configuration**: The `biome.json` file in the root is the source of truth. It is configured for maximum type safety and code style consistency.
- **Execution**: Run `bun lint` to check for issues and `bun format` to format the entire codebase. This is enforced by the CI pipeline.

### 3.2. Naming Conventions
- **Variables**: `camelCase`.
- **Functions**: `camelCase`.
- **Classes/Types/Interfaces**: `PascalCase`.
- **Files/Directories**: `kebab-case`. E.g., `src/http/user-controller.ts`.
- **Database Tables/Columns**: `snake_case`.
- **API Endpoints**: `kebab-case` for paths (`/user-profiles`), `camelCase` for JSON keys.

### 3.3. Import/Export Organization
- **Order**: 1. External libs, 2. Internal modules/aliases (`@/`), 3. Relative paths.
- Biome's linter enforces a consistent sort order.
- Use named exports over default exports to avoid naming conflicts.

### 3.4. Type Safety
- **TypeScript**: `tsconfig.json` is configured with `strict: true`. No implicit `any`.
- All functions and variables must have explicit types unless trivially inferred.
- Use utility types (e.g., `Pick`, `Omit`, `Readonly`) to create robust types.

### 3.5. Documentation and Commenting Standards
- **Motto**: "Why, not what." Code should be self-documenting.
- **When to comment**: Explain complex algorithms, business logic rationale, or workarounds.
- **Format**: Use JSDoc for all exported functions and classes.
    ```typescript
    /**
     * Registers a new user in the system.
     * @param email - The user's email address.
     * @param password - The user's raw password.
     * @returns A promise that resolves to the newly created user object.
     */
    ```

### 3.6. Error Handling and Logging
- **Errors**: Use custom error classes that extend `Error` for different failure scenarios (e.g., `ApiError`, `DatabaseError`).
- **Logging**: Use a structured JSON logger (e.g., Pino). Do not use `console.log` in application code.
- **Log Levels**: Use `info`, `warn`, `error` appropriately. `debug` logs are stripped in production.

## 4. Architecture and Design Principles

### 4.1. System Architecture Overview
- We use a **Hexagonal (Ports and Adapters) Architecture**.
- **Core Domain**: Contains business logic, completely independent of external frameworks.
- **Ports**: Interfaces defining interactions with the outside world (e.g., `UserRepository`).
- **Adapters**: Concrete implementations of ports (e.g., `PostgresUserRepository`, `RestApiController`).
- [Link to Architecture Diagram]

### 4.2. Design Patterns
- **Dependency Injection**: Dependencies are injected into classes/functions. We use a container like `tsyringe` or manual injection.
- **Repository Pattern**: Abstract data access logic behind repository interfaces.
- **Strategy Pattern**: For implementing interchangeable algorithms (e.g., different notification methods).

### 4.3. API Design
- **Primary Style**: RESTful conventions.
- **Specification**: All endpoints must be documented using the **OpenAPI 3.0** standard in `openapi.yaml`.
- **Versioning**: API is versioned in the URL: `/api/v1/...`.
- **Authentication**: All endpoints (except public ones) are secured using JWT Bearer tokens.

## 5. Testing and Quality Assurance Excellence Framework

### 5.1. Multi-tiered Testing Strategy
- **Unit Tests**: Test individual functions/classes in isolation. Target >90% code coverage.
- **Integration Tests**: Test interactions between components (e.g., API -> Database).
- **End-to-End (E2E) Tests**: Test critical user flows in a production-like environment.

### 5.2. Testing Frameworks
- **Unit/Integration**: Vitest. Run with `bun test`.
- **E2E**: Playwright. Run with `bun test:e2e`.
- **Mocks**: Use Vitest's built-in mocking capabilities.

### 5.3. Quality Gates and CI/CD Integration
- The CI pipeline fails if:
    1. Linting or formatting checks fail.
    2. Any test fails.
    3. Unit test coverage drops below 90%.
- A pull request cannot be merged unless the CI pipeline passes.

## 6. Security and Compliance Comprehensive Framework

### 6.1. Security Best Practices
- Follow **OWASP Top 10** guidelines.
- Use dependency scanning (`bun audit`, Snyk) to detect vulnerabilities in third-party packages.
- All infrastructure is managed via **Infrastructure as Code (IaC)** to ensure auditable and repeatable setups.

### 6.2. Authentication and Authorization
- **Auth**: JWT-based authentication (RS256 algorithm).
- **Authorization**: Role-Based Access Control (RBAC). A user's role and permissions are encoded in their JWT.

### 6.3. Data Protection
- **Encryption in Transit**: TLS 1.2+ is enforced for all external communication.
- **Encryption at Rest**: All databases and file storage are encrypted.
- **Passwords**: Are hashed using `argon2id`.

## 7. Operational Excellence and DevOps Comprehensive Framework

### 7.1. CI/CD Pipeline
- We use **GitHub Actions** for our CI/CD pipelines.
- **Workflow**: `Push -> Lint -> Test -> Build -> Deploy to Staging`.
- Merging to `main` triggers a deployment to production after manual approval.

### 7.2. Deployment Strategy
- **Staging**: Deploys automatically on merge to `develop`.
- **Production**: We use a **Blue-Green** deployment strategy to ensure zero-downtime releases.

### 7.3. Monitoring and Observability
- **Metrics**: Prometheus for collecting time-series metrics.
- **Logs**: Centralized logging via Grafana Loki or Datadog.
- **Tracing**: OpenTelemetry for distributed tracing to debug requests across services.
- **Dashboards**: Grafana for visualizing key application and system metrics.

### 7.4. Incident Response
- **Alerting**: PagerDuty for critical alerts.
- **On-Call**: A defined on-call rotation is managed in PagerDuty.
- **Post-mortems**: A blameless post-mortem is required for every SEV-1/SEV-2 incident.
