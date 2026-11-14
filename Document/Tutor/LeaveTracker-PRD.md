# Leave Tracker: Sample App PRD

Status: Draft v0.1 (Phase 0 of the AppBuilder Tutor plan)

## Purpose
"Leave Tracker" is the single sample app used across every AppBuilder Tutor lesson. It is built once with the AppBuilder agent (skeleton) and the Configurator agent (refinement). A snapshot is captured after each stage, and the snapshots become the lesson checkpoints.

It must be familiar to any newcomer and use every no-code tool: data model, form, formula, search, workflow, permissions.

## Users and roles
| Role | Can do |
|---|---|
| Employee | Create and submit own leave requests; see own requests |
| Manager | See requests pending approval; approve or reject |

## Data model: `LeaveRequest`
Platform vocabulary: this is a **Data Model** (a *Transaction* in code), type **Form Model**. Do not call it an "Entity": in this platform "Entity List Of Value" means a reusable list of dropdown choices (used here for LeaveType). See [Glossary.md](./Glossary.md).

| Field | Type | Rules |
|---|---|---|
| Employee | Text (defaults to current user) | Required |
| LeaveType | Dropdown | Required. Values: Annual, Sick, Personal, Unpaid |
| StartDate | Date | Required |
| EndDate | Date | Required, must be on or after StartDate |
| Days | Number | Calculated: EndDate - StartDate + 1. Read-only |
| Reason | Multi-line text | Optional |
| Status | Dropdown | Default Draft. Values: Draft, Submitted, Approved, Rejected |
| ManagerComment | Text | Optional, filled at approval |

> Column and field names above are the intended design. Per project rules, the AppBuilder agent's actual generated schema must be read back and recorded here (and in the lesson docs) before any code or snapshot refers to it.

## Screens
1. **New Leave Request** (entry form): grouped as Request (LeaveType, StartDate, EndDate, Days) and Details (Reason). Status shown read-only.
2. **My Requests** (list): the current user's requests, filter by Status and LeaveType, newest first.
3. **Pending Approval** (list, Manager): requests with Status = Submitted.

## Business rules
- Days is calculated automatically and cannot be edited.
- EndDate before StartDate shows the message "End date must be on or after the start date".
- A new request starts as Draft.

## Workflow
Draft -> (Employee submits) -> Submitted -> (Manager decides) -> Approved or Rejected. Status is updated at each step; the manager can add a comment.

## Out of scope
Leave balances, public holidays and weekends handling, email notifications, calendar integration.

## Build stages and lesson snapshots
| Stage | Built by | Content | Snapshot | Lesson |
|---|---|---|---|---|
| 1 | AppBuilder agent | App, data model, basic form, basic lists from this PRD | A | 2 |
| 2 | Configurator agent | Data model and datasource fixes (field types, LeaveType options, Status default) | B | 2-3 |
| 3 | Configurator agent | Form layout and UI (grouping, required, labels) | C | 3 |
| 4 | Configurator agent | Formulas and validation | D | 4 |
| 5 | Agent or by hand | List filters and approval workflow | E | 5-6 |

Lesson 1 (tour) starts from the finished app; lesson 7 (permissions, report, next steps) starts from snapshot E.

## Acceptance criteria
- An Employee can create, fill and submit a request; Days calculates correctly.
- Invalid dates are blocked with the message above.
- A Manager sees only Submitted requests in Pending Approval and can approve or reject.
- Each snapshot A-E can be re-applied to a clean sandbox app and matches the stage description.
- A "gap log" lists everything the agents could not do or got wrong.
