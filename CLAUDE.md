# OpenChat - Reglas de Desarrollo

## Arquitectura

**Clean Architecture es obligatoria en todo momento**, tanto para el API como para la UI.
Cada capa solo conoce a la capa inmediatamente inferior. Nunca al reves.

### Backend (C# / .NET)

Estructura de capas:
- `Domain` — Entidades, value objects, interfaces de repositorio, enums
- `Application` — Use cases, DTOs, interfaces de servicios, validaciones
- `Infrastructure` — Implementaciones de repositorios, acceso a base de datos, servicios externos
- `API` — Controllers, middleware, configuracion de DI

Estructura de folders (cada tipo en su carpeta):
```
Domain/
  Entities/
  Enums/
  Interfaces/
Application/
  UseCases/
  DTOs/
  Interfaces/
  Services/
Infrastructure/
  Repositories/
  Services/
  Persistence/
API/
  Controllers/
  Middleware/
```

### Frontend (Angular)

Estructura de capas:
- `core` — Servicios singleton, guards, interceptors, modelos globales
- `features` — Modulos funcionales (cada feature en su propio folder)
- `shared` — Componentes, pipes, directives reutilizables

Estructura de folders:
```
core/
  services/
  guards/
  interceptors/
  models/
features/
  <feature-name>/
    components/
    services/
    models/
shared/
  components/
  pipes/
  directives/
```

## Principios SOLID

Aplicar en todo momento, sin excepciones:

- **S** — Una clase, un motivo para cambiar. Los controllers no tienen logica de negocio.
- **O** — Extender por herencia/composicion, no modificar clases existentes.
- **L** — Las implementaciones deben poder sustituir a sus interfaces sin romper nada.
- **I** — Interfaces pequeñas y especificas. No forzar implementaciones innecesarias.
- **D** — Depender de abstracciones (interfaces), nunca de implementaciones concretas.

## Codigo duplicado

**Prohibido duplicar logica.** Si la misma logica aparece en mas de un lugar:
- Backend: crear un servicio compartido en `Application/Services/` o `Infrastructure/Services/`
- Frontend: crear un servicio en `core/services/` o un componente en `shared/`

Antes de escribir codigo nuevo, verificar si ya existe algo equivalente.

## Comentarios

No agregar comentarios salvo que el motivo no sea obvio por el nombre del simbolo.
Casos validos: workarounds, restricciones externas, invariantes no evidentes.
No documentar que hace el codigo, los nombres deben hacerlo solos.

## Convenciones generales

- Nombres descriptivos y en ingles
- No abreviaciones salvo las universales (id, url, dto, etc.)
- Un archivo por clase/interfaz/enum
- Los DTOs no son entidades; las entidades no salen de la capa Domain
- Los controllers solo reciben, delegan y responden — sin logica de negocio
