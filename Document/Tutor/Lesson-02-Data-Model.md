# Lesson 2: Create your data model (Leave Request)

Status: DRAFT v0.1, **not yet verified in a running app.** Written from the UI source and the Leave Tracker PRD. Steps marked **(verify)** must be walked through in the real UI before this lesson goes into the tutor. Field names below are the *intended* ones; replace them with the names the App Builder agent actually generates, as recorded in snapshot A.

## Goal
By the end you have a **Leave Request** data model in your own app, with the fields needed to ask for time off. You will not write code.

## What you need first
- Lesson 1 done: you have an empty application called "My Leave Tracker".
- The AI model is configured for your tenant (otherwise the assistant is not available). **(verify)**

## Ideas in 60 seconds
- A **data model** describes one kind of record: what you keep about each leave request.
- A **field** is one piece of information on it (a date, a choice, some text).
- The app stores the data for you; you only describe it.

## Why we use the assistant here
In the current screen, the manual ways to create a data model are:
- **From Database**: needs tables that already exist, so it is not for beginners.
- **From Form** and **From Excel**: show "not yet implemented" at the moment.

So the beginner route is to describe the data model to the AI assistant and let it build it, then check the result in Data Model Design.

## Steps
1. **Open the editor.** Open "My Leave Tracker" in My Application Editor. The tab is titled `Config: My Leave Tracker`. **(verify)**
2. **Open the assistant.** Until the Assistant dock exists in the editor, open the App Builder Agent page and choose your application. **(verify route)**
3. **Describe what you need**, in your own words. Example you can paste:
   > Create a Leave Request data model with these fields: Employee (text, required), Leave type (dropdown: Annual, Sick, Personal, Unpaid, required), Start date (date, required), End date (date, required), Days (number, calculated later), Reason (long text), Status (dropdown: Draft, Submitted, Approved, Rejected; default Draft), Manager comment (text).
4. **Confirm when asked.** The assistant shows what it is about to create and asks you to confirm. Read it, then confirm. **(verify wording)**
5. **Check the result.** Go to **Data Model Design** and press **Refresh**. You should see "Leave Request" in the grid under *1. Form Model*. **(verify)**
6. **Open it to look.** Use the pencil/menu button at the start of the row and open the editor to see the fields. **(verify the menu item name)**

## Check yourself
- [ ] "Leave Request" appears in Data Model Design.
- [ ] It has all eight fields, with Leave type and Status offered as dropdowns.
- [ ] Status starts as Draft.

Tutor check (read-only): the tutor can list the application's data models and compare them with the expected field list for this lesson.

## Common mistakes
- **Creating it in the wrong application.** Always check the tab title says `Config: My Leave Tracker`.
- **Typing "Days" as a normal field and then filling it in by hand.** It is calculated in lesson 4, so leave it empty now.
- **Looking for "Entity".** Here "Entity List Of Value" means a list of dropdown choices, not your Leave Request.
- **Dropdown choices missing.** If Leave type has no choices, ask the assistant to add them, or check **Entity List Of Value**. **(verify)**

## If you are stuck
Say "reset me to the start of lesson 3" and the tutor will load snapshot B. (Phase 3 feature; until then the facilitator loads it through Import Config.)

## Open questions for the reviewer
1. Does the current App Builder agent create dropdown choices itself, or via Entity List Of Value?
2. Which route should beginners use to reach the agent today (App Builder Agent page, or Agent Management Test Run)?
3. Is the Data Source choice shown to beginners, and which one should they pick?
