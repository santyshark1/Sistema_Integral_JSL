# Diagrama de Casos de Uso — JSL SentinelPro (corregido)

> Corrige la **Figura 20** del documento: `<<include>>` y `<<extend>>` bien dirigidos,
> sin casos de uso duplicados, con el actor **Administrador** reflejado y el límite del
> sistema explícito. Mermaid no tiene notación UML de casos de uso nativa, así que se
> representa con un grafo + convenciones marcadas.

```mermaid
flowchart LR
    User(("👤 Usuario"))
    Admin(("👤 Administrador"))

    subgraph SYS["🖥️ Sistema JSL SentinelPro (límite del sistema)"]
        direction TB
        Login["Iniciar sesión"]
        Recuperar["Recuperar contraseña"]
        Diag["Diagnosticar hardware"]
        Detectar["Detectar amenazas"]
        Mant["Ejecutar mantenimiento"]
        Reporte["Generar reporte PDF"]
        Guardar["Guardar resultado"]
        LeerHist["Leer historial"]
        Alertas["Generar alertas"]
        GestUsers["Gestionar usuarios"]
        ActBD["Actualizar BD de amenazas"]
    end

    User --> Login
    User --> Diag
    User --> Detectar
    User --> Mant
    User --> Reporte
    User --> Recuperar
    Admin --> GestUsers
    Admin --> ActBD

    %% include: el caso base SIEMPRE usa el incluido
    Diag -.->|"«include»"| Guardar
    Detectar -.->|"«include»"| Guardar
    Reporte -.->|"«include»"| LeerHist

    %% extend: el opcional extiende al base (flecha DESDE la extensión HACIA el base)
    Alertas -.->|"«extend»"| Detectar
    Alertas -.->|"«extend»"| Diag
```

## Correcciones respecto a la Figura 20 original

| Error original | Corrección aquí |
|---|---|
| `<<extend>>` mal dirigido | La flecha `«extend»` va **desde la extensión hacia el caso base**: `Generar alertas «extend» Detectar amenazas / Diagnosticar hardware`. |
| `Generar alertas «extend» Configurar sistema` (sin sentido) | Las alertas se disparan desde **detección/diagnóstico**, no desde configuración. |
| `Configurar sistema` duplicado | Cada caso de uso aparece **una sola vez**. |
| Faltaban `<<include>>` obvios | `Generar reporte «include» Leer historial`; `Detectar amenazas` y `Diagnosticar hardware` **«include» Guardar resultado**. |
| Actor **Administrador** no reflejado | Incluido con `Gestionar usuarios` y `Actualizar BD de amenazas`. |
| Límite del sistema difuso | Un solo rectángulo (`subgraph SYS`) contiene los casos de uso; los actores quedan **fuera**. |

> **Convención:** línea punteada `«include»` = el caso base siempre ejecuta el incluido;
> `«extend»` = comportamiento opcional que extiende al base (dirección extensión → base).