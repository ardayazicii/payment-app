# Payment Application

A full-stack payment application built with ASP.NET Core, Vue.js and TypeScript.

The project demonstrates card BIN recognition, installment options based on card and amount, 3D Secure payment flow, payment status tracking and persistent payment records.

The payment integration currently uses the iyzico Sandbox environment.

---

## Tech Stack

### Backend

- ASP.NET Core (.NET 10)
- C#
- Entity Framework Core
- SQLite
- iyzico .NET SDK
- REST API

### Frontend

- Vue 3
- TypeScript
- Vite

---

## Architecture

```text
Vue 3 + TypeScript
        |
        | REST API
        v
ASP.NET Core Web API
        |
        +----------------------+
        |                      |
        v                      v
BIN / Installment         Payment Service
     Service                   |
        |                      |
        +----------+-----------+
                   |
                   v
             iyzico Sandbox
                   |
                   v
              3D Secure
                   |
                   v
              Callback API
                   |
                   v
          SQLite / EF Core
```

The frontend never communicates directly with the payment provider.

All iyzico communication is handled by the backend.

---

## Features

### BIN Lookup

When the first 8 digits of a card number are entered, the backend retrieves card information from iyzico.

Example information:

```text
Bank: Garanti Bankası
Card Association: MASTER_CARD
Card Family: Bonus
Card Type: CREDIT_CARD
```

Endpoint:

```http
POST /api/cards/bin-lookup
```

Example request:

```json
{
  "bin": "54003600"
}
```

---

## Installment Lookup

Installment options are retrieved dynamically based on:

- Card BIN
- Payment amount
- iyzico merchant installment configuration

Endpoint:

```http
POST /api/cards/installments
```

Example request:

```json
{
  "bin": "54308100",
  "amount": 100
}
```

Example response:

```json
{
  "bankName": "Halkbank",
  "cardAssociation": "MASTER_CARD",
  "cardFamily": "Paraf",
  "cardType": "CREDIT_CARD",
  "installments": [
    {
      "installmentNumber": 1,
      "installmentPrice": 100.00,
      "totalPrice": 100.00
    },
    {
      "installmentNumber": 3,
      "installmentPrice": 35.08,
      "totalPrice": 105.25
    }
  ]
}
```

The backend validates the selected installment option again before starting the payment.

This prevents the frontend from manipulating the installment price or total payment amount.

---

## 3D Secure Payment

The application supports an end-to-end 3D Secure flow.

```text
Payment Form
    |
    v
POST /api/payments/3ds/start
    |
    v
iyzico 3D Secure Initialization
    |
    v
3D Secure Verification
    |
    v
POST /api/payments/3ds/callback
    |
    v
Payment Completion
    |
    v
Success / Failed
```

The callback signature is validated before completing the payment.

---

## Payment Records

Payment transactions are stored using SQLite and Entity Framework Core.

Endpoint:

```http
GET /api/payments
```

Example record:

```json
{
  "id": 7,
  "amount": 100,
  "paidPrice": 129.20,
  "installment": 12,
  "installmentPrice": 10.77,
  "status": "Success",
  "provider": "Iyzico",
  "bin": "54003600",
  "lastFour": "0003",
  "bankName": "Garanti Bankası",
  "cardAssociation": "MASTER_CARD",
  "cardFamily": "Bonus",
  "cardType": "CREDIT_CARD",
  "providerPaymentId": "38163857"
}
```

`Amount` represents the original transaction amount.

`PaidPrice` represents the total amount charged after installment pricing.

---

## Security

The application intentionally does not store:

```text
Full card number
CVV / CVC
```

Only the following card identifiers are persisted:

```text
BIN
Last four digits
Bank
Card association
Card family
Card type
```

iyzico API credentials are stored using ASP.NET Core User Secrets and are not committed to the repository.

---

## API Endpoints

```text
GET  /api/health

POST /api/cards/bin-lookup

POST /api/cards/installments

POST /api/payments/3ds/start

POST /api/payments/3ds/callback

GET  /api/payments
```

---

## Running the Backend

Go to:

```bash
cd backend/PaymentApi
```

Configure iyzico Sandbox credentials using User Secrets:

```bash
dotnet user-secrets set "Iyzico:ApiKey" "YOUR_SANDBOX_API_KEY"

dotnet user-secrets set "Iyzico:SecretKey" "YOUR_SANDBOX_SECRET_KEY"

dotnet user-secrets set "Iyzico:BaseUrl" "https://sandbox-api.iyzipay.com"
```

Apply database migrations:

```bash
dotnet ef database update
```

Run the backend:

```bash
dotnet run
```

Default development URL:

```text
http://localhost:5228
```

---

## Running the Frontend

Go to:

```bash
cd frontend/payment-ui
```

Install dependencies:

```bash
npm install
```

Run:

```bash
npm run dev
```

Default URL:

```text
http://localhost:5173
```

---

## Payment Provider

The current implementation uses iyzico.

Moka United was also evaluated as an alternative payment provider.

The application keeps payment-related logic in the service layer so that another provider can be introduced without moving payment logic into the frontend.

A possible future design could introduce a provider abstraction:

```text
IPaymentProvider

├── IyzicoPaymentProvider
└── MokaPaymentProvider
```

---

## Environment

This project uses the iyzico Sandbox environment only.

No real card information should be used while testing.