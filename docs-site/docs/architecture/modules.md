---
id: modules
title: Project Modules
---

- API: `src/API`
- Application: `src/Application`
- Domain: `src/Domain`
- Infrastructure: `src/Infrastructure`

Each layer follows dependency rules: outer layers depend on inner layers, never the opposite.
