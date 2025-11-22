# Sample Data Redesign Plan

## Current State Analysis

### Existing Infrastructure
1. **Seeding System**
   - Interface: `IDataSeeder` with `SeedAsync` method
   - Implementations:
     - `OutageCsvSampleDataSeeder` - Loads outages from CSV files
     - `TicketSeed` - Generates synthetic tickets, crews, and telemetry using Bogus library
   - Initialization: `DatabaseInitializationExtensions.InitializeDatabaseAsync()` runs on app startup

2. **CSV Sample Data**
   - Location: `sample-data/outages/`
   - Files: `outages.csv`, `outage-events.csv`
   - Data: Only 3 generic outages with vague scenarios
   - **Problem**: Not realistic for utility company workflows

3. **Synthetic Data (TicketSeed)**
   - Uses Bogus library to generate random data
   - Creates: 12 tickets, 8 crews, telemetry snapshots
   - Has ONE demo scenario: "Critical feeder fault near Hillcrest"
   - **Problem**: Random Hacker.Phrase() titles, Lorem.Paragraph() descriptions - completely unrealistic

4. **Migrations**
   - 4 migrations (InitialOutages, CsvSampleDataSeeding, AddTicketsAndDispatch, TicketConcurrencyFix)
   - **Problem**: Migrations have accumulated; schema changes are fragmented

### What's Missing
- ❌ No realistic utility company scenarios
- ❌ No neighborhood transformer accidents
- ❌ No electrical storm damage scenarios
- ❌ No construction-related power work
- ❌ No building demolition/new construction scenarios
- ❌ No realistic crew assignments matching actual dispatch workflows
- ❌ No realistic outage references (should be zone-based, e.g., "ZONE-NORTH-TX-047")
- ❌ Asset names are random street addresses instead of actual utility equipment (transformers, feeders, switches)

---

## Recommended Approach

### Option A: Enhanced Seeder-Only Approach ✅ **RECOMMENDED**

**Pros:**
- ✅ Faster startup (no migrations to run)
- ✅ Easier to modify scenarios without creating migrations
- ✅ Better for demos and development
- ✅ Can easily reset database (drop container, restart)
- ✅ Declarative scenario-based data in code
- ✅ Version control friendly (data definition in C# files)
- ✅ Can run on PostgreSQL OR in-memory for tests

**Cons:**
- Data recreated on every clean start
- Not suitable for production (but we don't need that)

**Implementation:**
1. Remove all migrations (we'll use `EnsureCreated` for development)
2. Remove CSV seeder infrastructure (too inflexible)
3. Create comprehensive `UtilitySampleDataSeeder` with realistic scenarios
4. Use declarative scenario builders for maintainability

### Option B: Keep Migrations

**Pros:**
- Schema versioning
- Production-ready migration path

**Cons:**
- ❌ Slower startup (migration validation)
- ❌ More complex to reset data
- ❌ Need new migration for every schema change
- ❌ Mixing schema changes with sample data is messy
- ❌ Overkill for a demo/development project

**Verdict:** Option A is better for GridPulse's use case.

---

## Realistic Scenarios to Implement

### 1. **Transformer Hit by Vehicle** 🚗💥
```
Title: "Vehicle collision with transformer TX-NORTH-442"
Reference: INCIDENT-NORTH-2024-TX442
Assets: TX-NORTH-442, FDR-NORTH-14A
Customer Impact: 340 homes
Priority: Critical
Status: CrewDispatched
Description: "Vehicle collision reported at Oak St & Maple Ave. Transformer TX-NORTH-442 
knocked off foundation. Visible oil leak. Feeder FDR-NORTH-14A auto-sectioned upstream."
Crew: Heavy Equipment Crew (skills: Transformer, HeavyEquipment, HighVoltage)
Location: 35.7796° N, 78.6382° W (Raleigh-like coordinates)
```

### 2. **Electrical Storm Damage** ⛈️
```
Title: "Multiple tree-on-line incidents - Storm Debbie"
Reference: STORM-DEBBIE-WEST-001
Assets: FDR-WEST-23A, FDR-WEST-23B, POLE-W-8833, POLE-W-8901
Customer Impact: 1,240 homes
Priority: High
Status: InProgress
Description: "Thunderstorm cell moved through West zone at 2:35 PM. Multiple reports of 
trees on distribution lines. Feeders 23A and 23B both reporting faults. SCADA shows 
recloser operations at multiple locations."
Crew: Line Crew Alpha (skills: Distribution, TreeTrimming, Underground)
```

### 3. **Scheduled Construction - New Development** 🏗️
```
Title: "New service connection - Meadowbrook Commons Phase 2"
Reference: PROJECT-EAST-MB-PHASE2
Assets: TX-EAST-NEW-091, UG-LATERAL-MB2-001
Customer Impact: 0 (planned work)
Priority: Medium
Status: Scheduled
Description: "Install new pad-mount transformer and underground laterals for Meadowbrook 
Commons subdivision Phase 2. 47 new residential lots. Scheduled outage window 6 AM - 11 AM."
Crew: Underground Crew Delta (skills: Underground, Transformer, Metering)
```

### 4. **Building Demolition - Service Disconnect** 🏚️
```
Title: "Service disconnect for demolition - Old Acme Warehouse"
Reference: DEMO-CENTRAL-ACME-001
Assets: TX-CENTRAL-229, METER-COM-4472
Customer Impact: 1 commercial
Priority: Low
Status: Scheduled
Description: "Disconnect 480V 3-phase service for Acme Warehouse demolition permit. 
Coordinate with city inspector. Remove meter and disconnect at transformer secondary."
Crew: Service Crew Bravo (skills: Metering, Commercial, HighVoltage)
```

### 5. **Equipment Failure - Aging Infrastructure** ⚙️
```
Title: "Transformer oil leak detected - TX-SOUTH-667"
Reference: MAINT-SOUTH-TX667
Assets: TX-SOUTH-667, FDR-SOUTH-08C
Customer Impact: 85 homes at risk
Priority: High
Status: Investigating
Description: "Routine patrol reported oil stains under TX-SOUTH-667. Visual inspection 
confirms active leak from gasket. Transformer installed 1987, due for replacement. 
SCADA temps normal but rising. Plan transfer and replacement."
Crew: Transformer Crew Echo (skills: Transformer, HighVoltage, SCADA)
```

### 6. **Underground Cable Fault** 🔌
```
Title: "Underground cable fault - Riverside Business Park"
Reference: FAULT-WEST-UG-RBP
Assets: UG-CABLE-RBP-MAIN, JUNCTION-RBP-J04
Customer Impact: 23 commercial customers
Priority: Critical
Status: InProgress
Description: "Primary underground cable fault detected in Riverside Business Park. 
Relay R-48 operated on ground fault. Estimated fault location between manholes 
MH-RBP-03 and MH-RBP-04. Network reconfigured to restore 18 customers. 5 still out."
Crew: Cable Crew Charlie (skills: Underground, Splicing, Testing)
```

### 7. **Wildlife Contact - Substation** 🦝
```
Title: "Substation breaker trip - wildlife contact suspected"
Reference: SUB-NORTH-WILDLIFE-001
Assets: SUB-NORTHRIDGE-MAIN, BREAKER-NR-M12
Customer Impact: 2,800 homes (brief outage)
Priority: Medium
Status: Resolved
Description: "Main breaker M12 at Northridge substation tripped at 3:47 AM. Successful 
reclose after 30 seconds. Inspection found evidence of raccoon contact on buswork. 
Animal guards installed. No equipment damage."
Crew: Substation Crew (skills: HighVoltage, Substation, Testing)
```

### 8. **Dig-In / Third Party Damage** 🚧
```
Title: "Underground cable struck - fiber optic installation crew"
Reference: DAMAGE-CENTRAL-FIBER-001
Assets: UG-CABLE-15KV-C44, SPLICE-C44-S09
Customer Impact: 127 homes
Priority: Critical
Status: CrewEnRoute
Description: "Third-party contractor installing fiber optic cable struck 15kV underground 
cable with boring equipment at intersection of Main St and 5th Ave. Cable de-energized. 
Contractor has USA 811 ticket on file. Splice repair required."
Crew: Emergency Response Crew (skills: Underground, Splicing, HotLine)
```

---

## Implementation Plan

### Phase 1: Clean Slate ✅
1. **Delete all migrations**
   ```powershell
   Remove-Item src/GridPulse.Infrastructure/Persistence/Migrations/*.cs
   ```

2. **Update DatabaseInitializationExtensions**
   - Always use `EnsureCreatedAsync()` (drop migration support)
   - Keep seeder pattern

3. **Remove CSV infrastructure**
   - Delete `OutageCsvSampleDataSeeder.cs`
   - Delete `sample-data/outages/` directory
   - Remove `SampleDataOptions` configuration

### Phase 2: Build Realistic Seeder 🏗️
1. **Create `UtilityScenarios.cs`** - Declarative scenario definitions
2. **Create `UtilitySampleDataSeeder.cs`** - Main seeder implementation
3. **Register in `ServiceCollectionExtensions`**

### Phase 3: Realistic Data Structure 📊
1. **Crews** (8-10 crews with specialized skills)
   - Heavy Equipment Crew (Transformer, HeavyEquipment, HighVoltage)
   - Line Crew Alpha (Distribution, TreeTrimming)
   - Line Crew Bravo (Distribution, HotLine)
   - Underground Crew Delta (Underground, Splicing, Cable)
   - Underground Crew Echo (Underground, Transformer, Metering)
   - Substation Crew (HighVoltage, Substation, Testing)
   - Emergency Response Crew (Underground, Splicing, HotLine)
   - Service Crew (Metering, Commercial, Residential)

2. **Service Territories**
   - North Zone (Raleigh area coordinates)
   - South Zone
   - East Zone
   - West Zone
   - Central Zone

3. **Asset Naming Conventions**
   - Transformers: `TX-{ZONE}-{NUMBER}` (e.g., TX-NORTH-442)
   - Feeders: `FDR-{ZONE}-{CIRCUIT}` (e.g., FDR-WEST-23A)
   - Poles: `POLE-{ZONE}-{NUMBER}` (e.g., POLE-W-8833)
   - Underground: `UG-{TYPE}-{IDENTIFIER}` (e.g., UG-CABLE-15KV-C44)
   - Substations: `SUB-{NAME}-{EQUIPMENT}` (e.g., SUB-NORTHRIDGE-MAIN)

4. **Outage Reference IDs**
   - Incidents: `INCIDENT-{ZONE}-{YEAR}-{ASSET}`
   - Storm: `STORM-{NAME}-{ZONE}-{SEQ}`
   - Projects: `PROJECT-{ZONE}-{NAME}`
   - Demolition: `DEMO-{ZONE}-{NAME}`
   - Maintenance: `MAINT-{ZONE}-{ASSET}`
   - Faults: `FAULT-{ZONE}-{TYPE}-{LOC}`
   - Damage: `DAMAGE-{ZONE}-{CAUSE}-{SEQ}`

### Phase 4: Data Relationships ✨
- Tickets reference real scenarios
- Crews have appropriate skills for ticket type
- Telemetry shows crews en route to their assigned tickets
- Assignment events track the workflow (created → recommendations generated → crew assigned → crew acknowledged → crew en route → resolved)
- Dispatch recommendations show multiple crew options with realistic scoring

### Phase 5: Configuration Simplification 🔧
**Update appsettings.json:**
```json
{
  "UtilitySampleData": {
    "Enabled": true,
    "IncludeScenarios": [
      "VehicleCollision",
      "StormDamage",
      "NewConstruction",
      "BuildingDemo",
      "EquipmentFailure",
      "UndergroundFault",
      "Wildlife",
      "ThirdPartyDamage"
    ],
    "CrewCount": 8,
    "ServiceTerritories": ["North", "South", "East", "West", "Central"]
  }
}
```

---

## Migration Strategy

### Development Workflow
```powershell
# Clean slate approach
docker-compose down -v  # Drop PostgreSQL container and volumes
docker-compose up -d    # Start fresh
aspire run              # App creates schema via EnsureCreated and seeds realistic data
```

### Testing Workflow
- Tests already use in-memory database
- `EnsureCreated()` works perfectly for in-memory
- No changes needed to test infrastructure

---

## Benefits of This Approach

### For Developers
✅ Clone repo → `aspire run` → Realistic data immediately  
✅ Easy to add new scenarios without migrations  
✅ Quick database resets during development  
✅ Clear, readable scenario definitions in code  

### For Demos
✅ Showcase realistic utility company workflows  
✅ Believable asset names and references  
✅ Accurate crew skill matching  
✅ Real-world incident types  

### For Architecture
✅ Clean separation: Schema (EF Core entities) vs. Data (seeders)  
✅ Testable (in-memory works the same)  
✅ Version controlled data scenarios  
✅ No migration cruft  

---

## Timeline Estimate

| Phase | Effort | Description |
|-------|--------|-------------|
| Phase 1 | 30 min | Delete migrations, update initialization |
| Phase 2 | 2-3 hours | Build scenario infrastructure |
| Phase 3 | 3-4 hours | Create 8 realistic scenarios with proper data |
| Phase 4 | 2 hours | Wire up relationships and events |
| Phase 5 | 30 min | Update configuration |
| **Total** | **8-10 hours** | Complete sample data overhaul |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Data too complex to maintain | Use builder pattern and scenario classes |
| Scenarios become stale | Document scenario intent, easy to update |
| Need migrations later for production | Can add back migrations when needed; keep seeder for dev |
| Tests break | Tests use in-memory, no impact expected |

---

## Decision: Proceed with Option A?

**Recommendation: YES** ✅

This approach gives you:
- Clean, maintainable sample data
- Realistic utility company scenarios
- Fast development cycles
- Easy onboarding for new developers
- Great demo experience

Ready to implement?
