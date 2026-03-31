# API Reference

## POST /api/evaluate

Evaluates an order against configured rulesets to determine the production plant.

### Request Body

```json
{
  "orderId": "1245101",
  "publisherNumber": "99999",
  "publisherName": "BookWorld Ltd",
  "orderMethod": "POD",
  "shipments": [
    {
      "shipTo": {
        "isoCountry": "US"
      }
    }
  ],
  "items": [
    {
      "sku": "PB-001",
      "printQuantity": 10,
      "components": [
        {
          "code": "Cover",
          "attributes": {
            "bindTypeCode": "PB"
          }
        }
      ]
    }
  ]
}
```

### Response (200 OK)

```json
{
  "matched": true,
  "productionPlant": "US",
  "matchedRuleset": "Ruleset Two",
  "matchedRule": "Rule 1",
  "reason": "Matched ruleset 'Ruleset Two', rule 'Rule 1'"
}
```

### Response (No Match)

```json
{
  "matched": false,
  "productionPlant": null,
  "matchedRuleset": null,
  "matchedRule": null,
  "reason": "No matching ruleset or rule found"
}
```

### Response (400 Bad Request)

```json
{
  "message": "Invalid order data",
  "details": ["..."]
}
```

### Response (500 Internal Server Error)

```json
{
  "message": "An internal error occurred",
  "details": ["..."]
}
```

## Context Fields Used in Evaluation

| Field | Source | Type |
|---|---|---|
| `PublisherNumber` | `order.publisherNumber` | string |
| `PublisherName` | `order.publisherName` | string |
| `OrderMethod` | `order.orderMethod` | string |
| `IsCountry` | `order.shipments[0].shipTo.isoCountry` | string |
| `PrintQuantity` | `order.items[0].printQuantity` | number |
| `Sku` | `order.items[0].sku` | string |
| `BindTypeCode` | `order.items[0].components[0].attributes.bindTypeCode` | string |
| `ComponentCode` | `order.items[0].components[0].code` | string |
