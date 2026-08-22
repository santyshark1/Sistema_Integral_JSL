# JSL SentinelPro

Aplicación de escritorio **Windows** para diagnóstico de hardware, mantenimiento
preventivo y ciberseguridad. Práctica IV — Universidad Central, Ingeniería de Sistemas.

## Stack
- **C# · .NET 8 · WPF** (`net8.0-windows`), arquitectura **MVVM**
- SQLite · LibreHardwareMonitorLib + WMI · Windows Defender · BCrypt · LiveCharts

## Requisitos
- Windows 10/11 (64-bit) — **la app solo se EJECUTA en Windows**
- .NET 8 SDK
- Ejecutar como Administrador (acceso a sensores y Defender)

## Compilar
```bash
dotnet restore
dotnet build -c Release
```
> Nota: el proyecto **compila** también en Linux/macOS (gracias a
> `EnableWindowsTargeting`), útil para CI, pero **solo se ejecuta en Windows**.

## Ejecutar (en Windows)
```bash
dotnet run
```

## Documentación
- `ESPECIFICACION.md` — estado del proyecto, arquitectura, evaluación del lenguaje, roadmap
- `docs/Diagrama_Secuencia.md` — diagramas de secuencia (corregidos)
- `DOCUMENTACION.md` — documentación técnica · `ManualUsuario.html` — manual de usuario

# Roadmap — Sistema Integral JSL

**Diagnóstico, Mantenimiento Preventivo y Ciberseguridad**

**Periodo:** 8 de agosto — 28 de noviembre de 2026 · 17 sábados

---

## Equipo y roles

| Inicial | Persona y rol principal en este tramo |
|---|---|
| **S** | Saray — Frontend / UX y redacción-forma del documento |
| **D** | David — Arquitectura y diagramas UML (líder técnico del documento) |
| **J** | Juan David — Backend / lógica, agente de diagnóstico y detección |
| **A** | Alejandro — Frontend / UX, mockups y referencias (APA) |

### Objetivo de la entrega final

**Última semana de noviembre:** documento corregido + software funcionando + sustentación oral.

**Prioridad del tramo:** corregir el documento (UML y coherencia de arquitectura), manteniendo el software en paralelo para poder demostrarlo.

> **Nota de planificación:** los dos últimos sábados son colchón (ensayo de sustentación e imprevistos). La corrección del stack va primero porque de ella dependen los diagramas, los requisitos y los mockups.

---

# Fase 1 — Cerrar la incoherencia de fondo (agosto)

### Meta

Eliminar la contradicción de arquitectura (Node vs Python vs .exe; web vs escritorio) y ajustar el alcance real del proyecto.

| Sábado | Foco del día | Tareas y responsables |
|---|---|---|
| **8 ago** | Decisión de arquitectura | **Reunión de los 4.** Acta con la decisión.<br><br>**Todos:** Decidir stack definitivo (recomendado: agente Python + web Node/Express).<br>**Todos:** Repartir las secciones del documento por responsable.<br>**D:** Modera la reunión y levanta el acta con la decisión tomada. |
| **15 ago** | Propagar el stack al texto | **J:** Reescribe §9.2.2 y §9.2.3 (capa de lógica) con el backend definitivo.<br>**A:** Corrige §7.1.4 (marco teórico) y §8 (metodología).<br>**S:** Ajusta §9.1.3 Sprint 1 y RNF-03 (compatibilidad).<br>**D:** Verifica que las tres secciones digan exactamente lo mismo. |
| **22 ago** | Alcance realista | **D:** Baja las afirmaciones sobre Ada Test a "telemetría por umbrales"; aclara que troyanos de hardware queda fuera de alcance (Obj. Específico 1 y Estado del arte).<br>**J:** Corrige el alcance de detección de malware (quitar "comportamiento", §9.1.5).<br>**A y S:** Eliminan vestigios: Java/C en la Solución y "SentinelPro" en los mockups. |
| **29 ago** | Buffer + revisión Fase 1 | **Cierre de mes.**<br><br>**Todos:** Lectura conjunta de lo corregido; validar que el stack es coherente de punta a punta. |

---

# Fase 2 — Diagramas UML (septiembre)

### Meta

Dejar los diagramas correctos en notación y coherentes con la arquitectura ya unificada.

| Sábado | Foco del día | Tareas y responsables |
|---|---|---|
| **5 sep** | Casos de uso | **D:** Rehace el Diagrama de Casos de Uso: corrige `<<extend>>`/`<<include>>`, elimina el "Configurar sistema" duplicado y reubica "Generar alertas".<br>**J:** Revisa que el diagrama refleje los requisitos funcionales reales. |
| **12 sep** | Secuencia | **D:** Rehace el Diagrama de Secuencia con el flujo real por JSON (el navegador no lee hardware; el agente entra como línea de vida).<br>**J:** Valida el flujo técnico paso a paso. |
| **19 sep** | Diagramas faltantes | **D y J:** Crean el Diagrama de Despliegue (agente / Vercel / Railway) — resuelve la confusión web vs escritorio.<br>**D y J:** Crean el Diagrama de Clases (modelo de dominio a partir del DER). |
| **26 sep** | Corregir el DER | **Buffer de cierre de mes.**<br><br>**J y D:** Añaden tabla `firmas_malware` y entidad `alertas`; arreglan cardinalidades de `reportes` y la relación `malware_detectado ↔ escaneos_hardware`. |

---

# Fase 3 — Software funcional (octubre)

### Meta

Tener la aplicación corriendo y alineada con lo que dice el documento.

> **Mes de mayor riesgo:** sin app no hay sustentación.

| Sábado | Foco del día | Tareas y responsables |
|---|---|---|
| **3 oct** | Arranque del sprint de código | **J:** Agente Python (`psutil` → JSON) funcionando.<br>**A:** Frontend base: login + dashboard.<br>**S:** Backend: endpoints iniciales.<br>**D:** Pone a punto entorno y despliegue. |
| **10 oct** | Núcleos técnicos | **J:** Parser del JSON + lógica de umbrales en el backend.<br>**A:** Panel de diagnóstico conectado a datos reales.<br>**S:** Módulo de detección por firmas (básico).<br>**D:** Base de datos en Railway operativa. |
| **17 oct** | Módulos restantes | **A y S:** Panel de ciberseguridad + generación de reportes PDF.<br>**J:** Motor de recomendaciones.<br>**D:** Integración de las capas + pruebas con Postman. |
| **24 oct** | Alinear software con documento | **D:** Verifica que el "tiempo real" se quitó o se implementó de verdad (coherencia mockups/RF vs app real).<br>**A y S:** Capturas nítidas de la app real para reemplazar los mockups viejos. |
| **31 oct** | Resultados de pruebas (§9.4) | **Buffer de cierre de mes.** Esta sección hoy está vacía.<br><br>**Todos:** Ejecutan pruebas en escenarios simulados y documentan resultados reales.<br>**D:** Redacta la sección §9.4 con los datos obtenidos. |

---

# Fase 4 — Pulido, forma y sustentación (noviembre)

### Meta

Forma impecable, referencias completas y sustentación ensayada.

| Sábado | Foco del día | Tareas y responsables |
|---|---|---|
| **7 nov** | Barrido de forma | **A:** Ortografía y gramática (Obsolescencia, sofisticado, "conceptos conceptos", coloquialismos → registro formal).<br>**S:** Unificar "ciberseguridad" y las comillas en todo el documento.<br>**D:** Numeración (falta §11) y orden de figuras. |
| **14 nov** | Referencias APA | **S:** Completar referencias faltantes (Nielsen, Sommerville, Booch, Silberschatz, Schwaber, Pressman).<br>**A:** Páginas en citas literales; corregir la cita de Zulkipli.<br>**J y D:** Verificar Figura 1 (2017 vs 2018) y cifras sin fuente. |
| **21 nov** | Integración final + ensayo 1 | **Todos:** Revisión completa del documento de punta a punta.<br>**Todos:** Primer ensayo de sustentación; repartir quién expone cada parte.<br>**D:** Coordina la integración final. |
| **28 nov** | Semana de entrega | **Colchón intencional:** aquí no debería quedar trabajo nuevo, solo pulir.<br><br>**Todos:** Ensayo final de sustentación, últimos ajustes de imprevistos y entrega. |

---

# Notas de riesgo

### Octubre es el punto crítico

Si el software se atrasa, la sustentación se complica porque no hay app que mostrar.

Aunque la prioridad declarada es el documento, la Fase 3 lleva margen y a los 4 involucrados. Si en septiembre el código va lento, adelantar trabajo de agente a **J**.

### Carga de D en septiembre

Los 4 diagramas + el DER recaen en **David**. Es intencional por el perfil técnico, pero si se satura, el **Diagrama de Clases (19 sep)** es el más delegable a **J**.
