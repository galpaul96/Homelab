# Licensing Client Workflow

The Licensing feature now manages Client records only. A Client represents an external entity with a name, description, and notes.

## Web Flow

- Open `/licensing` as an authenticated Admin user to view the Client catalog.
- Use **New client** to create a Client with a required name and optional description and notes.
- Click a Client card to open `/licensing/clients/{id}`.
- The details page shows the Client id, external id, name, created date, updated date, description, and notes.
- Only description and notes are editable after creation.
- Deleting a Client removes it from the catalog using the API soft-delete behavior.

## API Flow

The web service calls the API through the gateway using these Client endpoints:

- `GET /Licensing/clients`
- `GET /Licensing/clients/{id}`
- `POST /Licensing/clients`
- `PUT /Licensing/clients/{id}`
- `DELETE /Licensing/clients/{id}`

Client names are required but not unique. Existing product, license key, issuance rule, activation, and validation operations have been removed from this feature.
