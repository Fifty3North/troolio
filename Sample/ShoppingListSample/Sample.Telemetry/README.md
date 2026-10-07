# Shopping List browser client

A Vue 3 and TypeScript client for the public-package Shopping List sample. It demonstrates verified demo identities, collaborative list commands, paged SQL catalogs and live tracing.

## Start

Use Node.js 22.12 or newer. First start the [Shopping API and silo](../README.md), then:

```sh
cd Sample/ShoppingListSample/Sample.Telemetry
npm ci
npm run dev
```

Open `http://localhost:5173`. The checked-in development API URL is `http://localhost:8081/`; set `VITE_API_URL` when using another local API address. The API allows the documented localhost frontend origins.

Choose Alice, create a list and copy its join code. Choose Bob, join using the code, and refresh to see the collaboration projection. Authors see invite codes; collaborators do not. Previous/Next reads bounded pages of 50 lists.

## Follow a command

```typescript
await http.post(`ShoppingList/${listId}/AddItemToList`, {
  itemId: crypto.randomUUID(), description: 'Oat milk', quantity: '2'
});
```

`src/api.ts` attaches the selected account's Bearer demo token, a diagnostic device ID and a new request correlation ID. The API derives authority from the established demo principal. Client IDs do not grant permissions.

A command response confirms the actor operation; live projections can lag it. Refresh shows the current relational view. Request failures appear in the interface.

## Inspect tracing

Open **Tracing**, select a trace level and enable logging. Switch to Shopping Lists, send a command, return to Tracing and choose Flush. Select a message to inspect its payload and lineage. Cancel polling and disable logging when finished. The view retains its state when switching tabs and cancels its timer when the selected user changes or the component unmounts.

Tracing is a local teaching tool. All sample accounts are publicly known and the API refuses production startup until its demonstration authentication is replaced.

## Build

```sh
npm run build
npm run preview
```

The committed npm lockfile is authoritative. TypeScript stays on 5.9 because the current Vue type checker is incompatible with TypeScript 7's compiler export layout. Vite and Vue are kept on their current compatible releases.
