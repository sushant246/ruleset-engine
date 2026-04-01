# File-Watcher Quick Start Guide

## Running the File-Watcher Service

### Prerequisites
- .NET 10 SDK installed
- RulesetConfig.json populated with rulesets
- Folder structure created or auto-created by service

### Start the Service

```bash
# From project root
dotnet run --project src/RulesetEngine.FileWatcher/
```

**Expected Output:**
```
2025-01-02 12:00:05.123 [INF] FileWatcher watching: C:\Users\gawas\orders\incoming
2025-01-02 12:00:05.124 [INF] ✅ Database ready. Checking for rulesets...
2025-01-02 12:00:05.125 [INF] 📊 Seeding from config...
```

### Folder Structure (Auto-Created)

```
orders/
├── incoming/          ← Drop ZIP files here
├── archive/           ← Successful ZIPs
└── error/             ← Failed ZIPs
```

Override locations in `appsettings.json`:
```json
{
  "FileWatcher": {
    "WatchFolder": "path/to/incoming",
    "ArchiveFolder": "path/to/archive",
    "ErrorFolder": "path/to/error"
  }
}
```

---

## Creating Test ZIP Files

### Option 1: PowerShell (Windows)

```powershell
# Create test order JSON
$order = @{
    orderId = "TEST-001"
    publisherNumber = "99999"
    publisherName = "Test Publisher"
    orderMethod = "POD"
    shipments = @(@{
        shipTo = @{ isoCountry = "US" }
    })
    items = @(@{
        sku = "TEST-SKU-001"
        printQuantity = 15
        components = @(@{
            code = "Cover"
            attributes = @{ bindTypeCode = "PB" }
        })
    })
} | ConvertTo-Json

# Save order to temp file
$jsonFile = [System.IO.Path]::GetTempFileName()
$order | Set-Content $jsonFile

# Create ZIP
$zipPath = "orders/incoming/test-order.zip"
Compress-Archive -Path $jsonFile -DestinationPath $zipPath -Force

# Cleanup
Remove-Item $jsonFile
```

### Option 2: PowerShell (Multi-Order ZIP)

```powershell
function Create-MultiOrderZip {
    param([string]$zipPath, [int]$orderCount = 3)
    
    $tempDir = [System.IO.Path]::GetTempPath() + [guid]::NewGuid()
    mkdir $tempDir | Out-Null
    
    for ($i = 1; $i -le $orderCount; $i++) {
        $order = @{
            orderId = "BATCH-$i".PadRight(8, '0')
            publisherNumber = "99999"
            orderMethod = "POD"
            shipments = @(@{ shipTo = @{ isoCountry = "US" } })
            items = @(@{
                sku = "SKU-$i"
                printQuantity = 10 + $i
                components = @(@{
                    code = "Cover"
                    attributes = @{ bindTypeCode = "PB" }
                })
            })
        } | ConvertTo-Json
        
        $order | Set-Content "$tempDir/order-$i.json"
    }
    
    Compress-Archive -Path "$tempDir/*" -DestinationPath $zipPath -Force
    Remove-Item $tempDir -Recurse
}

# Create 3-order ZIP
Create-MultiOrderZip "orders/incoming/batch.zip" 3
```

### Option 3: Bash (Linux/macOS)

```bash
#!/bin/bash

# Create order JSON
cat > order.json << 'EOF'
{
  "orderId": "TEST-001",
  "publisherNumber": "99999",
  "publisherName": "Test Publisher",
  "orderMethod": "POD",
  "shipments": [{"shipTo": {"isoCountry": "US"}}],
  "items": [{
    "sku": "TEST-SKU-001",
    "printQuantity": 15,
    "components": [{"code": "Cover", "attributes": {"bindTypeCode": "PB"}}]
  }]
}
EOF

# Create ZIP
zip orders/incoming/test-order.zip order.json
rm order.json
```

---

## Testing Workflow

### Step 1: Start the Service
```bash
dotnet run --project src/RulesetEngine.FileWatcher/
```

Leave this running in a terminal.

### Step 2: Create and Drop Test ZIP
```powershell
# In another terminal
$order = @{
    orderId = "ORD-002"
    publisherNumber = "99999"
    orderMethod = "POD"
    shipments = @(@{ shipTo = @{ isoCountry = "US" } })
    items = @(@{
        sku = "BOOK-001"
        printQuantity = 22
        components = @(@{
            code = "Cover"
            attributes = @{ bindTypeCode = "PB" }
        })
    })
} | ConvertTo-Json

$json = [System.IO.Path]::GetTempFileName()
$order | Set-Content $json

Compress-Archive -Path $json -DestinationPath "orders/incoming/order-002.zip"
Remove-Item $json
```

### Step 3: Observe Processing

Watch the file-watcher terminal for logs:
```
2025-01-02 12:15:30.456 [INF] Processing ZIP: order-002.zip
2025-01-02 12:15:30.567 [INF] Processed order.json from order-002.zip: OrderId=ORD-002 → Plant=US (Matched=true, FallbackUsed=false)
2025-01-02 12:15:30.678 [INF] Moved C:\orders\incoming\order-002.zip → C:\orders\archive\order-002.zip
```

### Step 4: Verify Results

**Check archived ZIP:**
```powershell
Get-ChildItem orders/archive/
```

**Query evaluation logs (if API is running):**
```bash
curl http://localhost:7100/api/logs?count=5
```

**Check database directly:**
```bash
dotnet run --project src/RulesetEngine.Api/
# Then visit http://localhost:7100/swagger
# Try GET /api/logs
```

---

## Expected Behaviors

### ✅ Success Case
```
Input:  orders/incoming/batch.zip (valid orders)
  ↓
Process: Extract → Evaluate → Log
  ↓
Output: orders/archive/batch.zip
```

### ⚠️ Partial Failure
```
Input:  orders/incoming/mixed.zip (2 valid, 1 malformed JSON)
  ↓
Process: 
  - order1.json: OK → Evaluated
  - order2.json: ERROR → Skipped with warning
  - order3.json: OK → Evaluated
  ↓
Output: orders/archive/mixed.zip (ZIP moved despite error)
```

### ❌ Total Failure
```
Input:  orders/incoming/corrupted.zip (invalid ZIP format)
  ↓
Process: Can't extract
  ↓
Output: orders/error/corrupted.zip
```

---

## Troubleshooting

### Issue: "ZIP not being detected"

**Solution 1**: Check folder permissions
```powershell
Get-Acl orders/incoming | Format-List
```

**Solution 2**: Verify file is complete (wait 1-2 seconds after creating)
```powershell
Start-Sleep -Seconds 2
Move-Item test.zip orders/incoming/
```

**Solution 3**: Check file isn't locked
```powershell
# Ensure ZIP isn't open in another app
lsof orders/incoming/*.zip  # macOS/Linux
```

### Issue: "Evaluation returned Matched=false"

**Cause**: Order data doesn't match ruleset conditions

**Solution**: Verify order fields match config:
```bash
# From RulesetConfig.json:
# "field": "PublisherNumber", "operator": "Equals", "value": "99999"

# Your order MUST have:
# "publisherNumber": "99999"
```

### Issue: "Service crashes on start"

**Solution**: Check logs and ensure:
- RulesetConfig.json exists in `src/RulesetEngine.Api/`
- Database can be created (write permissions)
- Port is not in use

```bash
netstat -tuln | grep 5000  # Check port availability
```

---

## Performance Tuning

### High Volume Processing

For processing many ZIPs, consider:

1. **Batch mode** (multiple ZIPs in parallel)
2. **Parallel order processing** (within ZIP)
3. **Database connection pooling**

Current bottleneck: Sequential processing. Contact dev for async options.

### Monitoring

Track these metrics:
- ZIP files processed per minute
- Average evaluation time per order
- Error rate (% failed ZIPs)
- Database query performance

---

## Integration with API

The file-watcher writes logs to the same database as the API. Query results:

```bash
# Start API
dotnet run --project src/RulesetEngine.Api/

# Query recent evaluations
curl http://localhost:7100/api/logs?count=10
```

Response:
```json
[
  {
    "id": 1,
    "orderId": "ORD-002",
    "matchedRuleset": "Ruleset Two",
    "matchedRule": "Rule 3 - PB US Large Order",
    "productionPlant": "KGL",
    "matched": true,
    "fallbackUsed": false,
    "reason": "Matched ruleset 'Ruleset Two', rule 'Rule 3 - PB US Large Order'",
    "evaluatedAt": "2025-01-02T12:15:30Z"
  }
]
```

---

## Unit Testing

Run file-watcher tests:
```bash
dotnet test tests/RulesetEngine.Tests/FileWatcher/OrderFileProcessorTests.cs -v
```

**Test Coverage:**
- ✅ Single order processing
- ✅ Batch (multiple orders in one ZIP)
- ✅ Error handling
- ✅ File movement (success/error cases)
- ✅ Collision handling
- ✅ Cancellation
- ✅ Empty ZIPs

---

## Next Steps

1. ✅ Start the service
2. ✅ Create test ZIPs
3. ✅ Drop in `orders/incoming/`
4. ✅ Monitor logs
5. ✅ Verify results in archive
6. ✅ Query results via API

---

**Quick Reference:**

| Action | Command |
|--------|---------|
| Start service | `dotnet run --project src/RulesetEngine.FileWatcher/` |
| Run tests | `dotnet test tests/RulesetEngine.Tests/FileWatcher/` |
| Check folder | `Get-ChildItem orders/incoming/` |
| Query logs | `curl http://localhost:7100/api/logs` |

