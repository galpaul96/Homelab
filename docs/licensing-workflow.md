# Licensing Client And Product Workflow

The Licensing feature manages Client records and Products linked to those Clients. A Client represents an external entity. A Product represents a named product owned by a Client, with optional description, type, and hosting information.

## Web Flow

- Open `/licensing` as an authenticated Admin user to view the Client catalog.
- Use **New client** to create a Client with a required name and optional description and notes.
- Click a Client card to open `/licensing/clients/{id}`.
- The Client details page shows compact Client metadata, editable description and notes, and a Product card grid.
- Use **New product** on the Client details page to create a Product linked to that Client.
- Click a Product card to open `/licensing/products/{productId}`.
- Product details show readonly Client fields and editable Product fields.
- Product deletion returns to the owning Client details page.
- Client deletion is blocked while visible Products exist for that Client.

## API Flow

Client endpoints:

- `GET /Licensing/clients`
- `GET /Licensing/clients/{id}`
- `POST /Licensing/clients`
- `PUT /Licensing/clients/{id}`
- `DELETE /Licensing/clients/{id}`

Product endpoints:

- `GET /Products?clientId={clientId}`
- `GET /Products/{id}`
- `POST /Products`
- `PUT /Products/{id}`
- `DELETE /Products/{id}`

Client and Product names are required but not unique. Product `Type` and `HostedOn` are free-form strings.
