# 🗺️ Roadmap — Práctica IV (Ago → Nov 2026)

**Equipo:** 🟦 **S** Saray · 🟩 **D** David · 🟨 **J** Juan David · 🟧 **A** Alejandro
**Entrega final:** última semana de noviembre — documento + software + sustentación.

---

## Línea de tiempo

```mermaid
gantt
    title Práctica IV — 17 sábados
    dateFormat YYYY-MM-DD
    axisFormat %d %b

    section 🧭 Coherencia
    Decidir stack (C#/.NET escritorio)   :done, f1, 2026-08-08, 7d
    Propagar stack al documento          :active, f2, 2026-08-15, 14d
    Revisión Fase 1                      :f3, 2026-08-29, 7d

    section 📐 Diagramas UML
    Casos de uso + Secuencia             :u1, 2026-09-05, 14d
    Despliegue + Clases + DER            :u2, 2026-09-19, 14d

    section 💻 Software
    Alinear app con el documento         :crit, s1, 2026-10-03, 21d
    Resultados de pruebas (§9.4)         :crit, s2, 2026-10-24, 14d

    section ✨ Cierre
    Forma + Referencias APA              :c1, 2026-11-07, 14d
    Integración + ensayo sustentación    :milestone, c2, 2026-11-21, 0d
    Semana de entrega (colchón)          :c3, 2026-11-21, 14d
```

---

## Fases de un vistazo

| Fase | Cuándo | Foco | Líder |
|:--:|---|---|:--:|
| 1 | **Agosto** | Cerrar la incoherencia del stack y bajar alcance (Ada Test, malware) | 🟩 D |
| 2 | **Septiembre** | Diagramas UML: casos de uso, secuencia, despliegue, clases, DER | 🟩 D |
| 3 | **Octubre** 🔴 | Software funcional alineado al documento — **ruta crítica** | 🟨 J |
| 4 | **Noviembre** | Pulido, APA, ensayo de sustentación (2 sábados de colchón) | 🟦 S / 🟧 A |

> 🔴 **Octubre es el punto crítico:** sin app funcional no hay sustentación.
> Los 2 últimos sábados son colchón intencional para imprevistos y ensayo.

---

## Roles

| | Integrante | Responsabilidad |
|:--:|---|---|
| 🟦 **S** | Saray | Frontend/UX · redacción y forma del documento |
| 🟩 **D** | David | Arquitectura y diagramas UML (líder técnico del documento) |
| 🟨 **J** | Juan David | Backend/lógica · agente de diagnóstico y detección |
| 🟧 **A** | Alejandro | Frontend/UX · mockups y referencias (APA) |
