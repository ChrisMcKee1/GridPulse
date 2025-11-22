# Dispatch Workflow Guide

## Overview
The Dispatch Board helps utility dispatchers assign the right crew to power outages and incidents using AI-powered recommendations.

## User Flow

### 1. Select an Incident
- **Left panel**: Shows all open tickets (outages, downed lines, equipment failures)
- **Click a ticket** to see AI-recommended crews on the right

### 2. Review AI Recommendations
- **⭐ Star icon** = AI-recommended "best match" based on:
  - **Proximity**: Closest crew to incident location
  - **Skills**: Crew capabilities match incident requirements (e.g., "Tree removal" for storm damage)
  - **Workload**: Crew's current active ticket count
  - **Availability**: Crew status (available, busy, off-duty)
  - **Telemetry freshness**: Real-time GPS location data quality

- **Score column**: Overall match percentage (higher = better fit)
- **ETA column**: Estimated arrival time in minutes
- **Workload column**: How many active tickets the crew already has

### 3. Assign a Crew

#### Scenario A: Using the Recommended Crew (Easy Path)
1. The top crew is **auto-selected** with a ⭐ star
2. Click the green **"Assign [Crew Name]"** button
3. ✅ Assignment queued immediately - crew receives push notification

#### Scenario B: Choosing a Different Crew (Override Path)
1. Click a **different row** in the crew recommendations table
2. 📋 **Blue info alert appears**: "Override required - you've selected [Crew Name] instead of the AI-recommended crew"
3. Button changes to orange: **"Provide reason to assign [Crew Name]"**
4. Click the button → **Override dialog opens**
5. Enter justification (e.g., "Crew 2 has specialized high-voltage equipment needed for this transformer")
6. Click **"Confirm override"**
7. ✅ Assignment queued with override reason logged

### 4. Track Assignment Progress
- **Timeline panel** (bottom right): Shows real-time crew status updates
  - "Dispatched" → "En route" → "On site" → "Work started" → "Completed"
- **Crew Status Panel**: Shows active crew location and allows status updates

## Visual Indicators

| Icon | Meaning |
|------|---------|
| ⭐ **Star icon** | AI-recommended best match |
| 🟢 **Green button** | Standard assignment (using recommendation) |
| 🟠 **Orange button** | Override assignment (requires justification) |
| ⚠️ **Warning badge** | Telemetry stale (GPS data >5 minutes old) |
| ℹ️ **Blue alert** | Override justification required |

## Business Rules

### When Override is Required
- Selecting **any crew except the starred recommendation**
- System will **automatically prompt** for justification
- Override reasons are **logged** for audit and quality improvement

### Why Overrides Matter
1. **Compliance**: Regulatory audits require documented crew assignment decisions
2. **AI Training**: Your override reasons help improve future recommendations
3. **Accountability**: Supervisors can review override patterns to identify issues

## Example Scenarios

### Scenario 1: Simple Downed Line
- **Incident**: Tree branch on power line, 120V residential
- **Recommended Crew**: "North Valley Line Crew" (99% match, 12 min ETA, 0 active tickets)
- **Action**: Click green "Assign North Valley Line Crew" button
- **Result**: Crew dispatched immediately

### Scenario 2: High-Voltage Emergency
- **Incident**: Transformer fire, 69kV substation equipment
- **Recommended Crew**: "East Metro Response" (95% match, 8 min ETA)
- **Problem**: East Metro lacks specialized high-voltage certification
- **Action**: 
  1. Select "HV Specialists Team" (87% match, 18 min ETA)
  2. Click orange "Provide reason..." button
  3. Enter: "Requires 69kV certified crew for substation work per safety protocol SP-441"
  4. Confirm override
- **Result**: HV Specialists dispatched with documented justification

### Scenario 3: Workload Balancing
- **Incident**: Routine meter replacement
- **Recommended Crew**: "Central Maintenance" (98% match, 5 min ETA, **4 active tickets**)
- **Decision**: Override to "South District Crew" (92% match, 8 min ETA, **0 active tickets**)
- **Justification**: "Balancing workload - Central crew already handling 4 tickets"
- **Result**: Better resource distribution across teams

## Tips for Dispatchers

✅ **DO:**
- Trust the AI for routine incidents - it considers factors you might miss
- Provide detailed override reasons (helps improve the system)
- Check telemetry staleness warnings before assigning
- Use workload scores to balance crew utilization

❌ **DON'T:**
- Override based on personal preferences without valid reasons
- Ignore telemetry warnings (stale GPS = crew might not be where shown)
- Assign crews to incidents requiring skills they don't have

## Technical Notes

### Recommendation Refresh
- Recommendations **auto-refresh every 45 seconds**
- Crew locations and workloads update in real-time
- Stale recommendations (>5 minutes old) require refresh before assignment

### Assignment Queue
- Assignments are **asynchronous** - queued for delivery
- Crew receives **push notification** on mobile device
- Tracking ID provided for audit trail

---

**Questions?** Contact your system administrator or refer to the [Architecture Documentation](./architecture.md).
