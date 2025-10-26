# Fidelity Points API Testing Guide

## Overview
This document provides testing instructions for the Fidelity Points & Customer Discount system implemented in Phases 1-3.

## Implementation Status

### ✅ Phase 1: Database & Domain Layer (COMPLETE)
- Created 4 new tables: `fidelity_point_balances`, `fidelity_points_transactions`, `point_earning_rules`, `customer_discount_rules`
- Extended `orders` table with 5 fidelity columns
- Database migration applied successfully: `20251025200934_AddFidelityPointsAndDiscounts`
- Seeder creates 4 default point earning rules on startup

### ✅ Phase 2: Backend Services & Business Logic (COMPLETE)
- `FidelityPointsService` - Point calculations, awarding, redemption (100 points = $1)
- `PointEarningRuleService` - CRUD operations with overlap validation
- `CustomerDiscountService` - Discount management with usage tracking
- Integrated with `CreateOrderCommandHandler` for automatic point earning

### ✅ Phase 3: Backend API Endpoints (COMPLETE)
- User endpoints: `/api/FidelityPoints/*`
- Admin endpoints: `/api/admin/PointRules/*`, `/api/admin/CustomerDiscounts/*`
- FluentValidation for all DTOs

---

## API Endpoints

### User-Facing Endpoints (Authentication Required)

#### 1. Get User's Point Balance
```http
GET /api/FidelityPoints/balance
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    "id": "guid",
    "userId": "guid",
    "currentPoints": 500,
    "totalEarnedPoints": 1200,
    "totalRedeemedPoints": 700,
    "lastUpdated": "2025-10-25T10:30:00Z",
    "currentPointsValue": 5.00
  }
}
```

#### 2. Get Points Transaction History
```http
GET /api/FidelityPoints/history?page=1&pageSize=50
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "userId": "guid",
      "orderId": "guid",
      "transactionType": "Earned",
      "points": 15,
      "orderTotal": 30.00,
      "description": "Points earned from order",
      "createdAt": "2025-10-25T10:00:00Z"
    },
    {
      "id": "guid",
      "userId": "guid",
      "orderId": "guid",
      "transactionType": "Redeemed",
      "points": -100,
      "description": "Points redeemed for $1.00 discount",
      "createdAt": "2025-10-24T15:30:00Z"
    }
  ]
}
```

#### 3. Calculate Discount from Points
```http
GET /api/FidelityPoints/calculate-discount?points=500
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": 5.00
}
```

#### 4. Calculate Points Needed for Discount
```http
GET /api/FidelityPoints/calculate-points?discountAmount=10.00
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": 1000
}
```

---

### Admin Endpoints (Admin Role Required)

#### 5. List All Point Earning Rules
```http
GET /api/admin/PointRules?activeOnly=false
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "name": "Bronze Tier",
      "minOrderAmount": 0.00,
      "maxOrderAmount": 20.00,
      "pointsAwarded": 5,
      "isActive": true,
      "priority": 0,
      "createdAt": "2025-10-25T00:00:00Z"
    },
    {
      "id": "guid",
      "name": "Silver Tier",
      "minOrderAmount": 20.00,
      "maxOrderAmount": 50.00,
      "pointsAwarded": 15,
      "isActive": true,
      "priority": 1,
      "createdAt": "2025-10-25T00:00:00Z"
    },
    {
      "id": "guid",
      "name": "Gold Tier",
      "minOrderAmount": 50.00,
      "maxOrderAmount": 100.00,
      "pointsAwarded": 30,
      "isActive": true,
      "priority": 2,
      "createdAt": "2025-10-25T00:00:00Z"
    },
    {
      "id": "guid",
      "name": "Platinum Tier",
      "minOrderAmount": 100.00,
      "maxOrderAmount": null,
      "pointsAwarded": 60,
      "isActive": true,
      "priority": 3,
      "createdAt": "2025-10-25T00:00:00Z"
    }
  ]
}
```

#### 6. Create Point Earning Rule
```http
POST /api/admin/PointRules
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Diamond Tier",
  "minOrderAmount": 200.00,
  "maxOrderAmount": null,
  "pointsAwarded": 120,
  "isActive": true,
  "priority": 4
}
```

**Response:**
```json
{
  "success": true,
  "message": "Point earning rule created successfully",
  "data": {
    "id": "new-guid",
    "name": "Diamond Tier",
    "minOrderAmount": 200.00,
    "maxOrderAmount": null,
    "pointsAwarded": 120,
    "isActive": true,
    "priority": 4,
    "createdAt": "2025-10-25T12:00:00Z"
  }
}
```

#### 7. Update Point Earning Rule
```http
PUT /api/admin/PointRules/{ruleId}
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Gold Tier - Updated",
  "minOrderAmount": 50.00,
  "maxOrderAmount": 100.00,
  "pointsAwarded": 35,
  "isActive": true,
  "priority": 2
}
```

#### 8. Delete Point Earning Rule
```http
DELETE /api/admin/PointRules/{ruleId}
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Point earning rule deleted successfully"
}
```

#### 9. Validate Point Rule (Check for Overlaps)
```http
POST /api/admin/PointRules/validate
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Test Rule",
  "minOrderAmount": 25.00,
  "maxOrderAmount": 40.00,
  "pointsAwarded": 10,
  "isActive": true,
  "priority": 5
}
```

**Response:**
```json
{
  "success": true,
  "message": "Rule overlaps with existing rules",
  "data": false
}
```

#### 10. List All Customer Discounts
```http
GET /api/admin/CustomerDiscounts?userId={optional}&activeOnly=false
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "userId": "guid",
      "userEmail": "customer@example.com",
      "userName": "John Doe",
      "name": "VIP Customer 20% Off",
      "discountType": "Percentage",
      "discountValue": 20.00,
      "minOrderAmount": 30.00,
      "maxOrderAmount": null,
      "maxUsageCount": 5,
      "usageCount": 2,
      "isActive": true,
      "validFrom": "2025-10-01T00:00:00Z",
      "validUntil": "2025-12-31T23:59:59Z",
      "createdAt": "2025-10-01T00:00:00Z"
    }
  ]
}
```

#### 11. Create Customer Discount
```http
POST /api/admin/CustomerDiscounts
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "userId": "customer-guid",
  "name": "Holiday Bonus 10% Off",
  "discountType": "Percentage",
  "discountValue": 10.00,
  "minOrderAmount": 25.00,
  "maxOrderAmount": null,
  "maxUsageCount": 3,
  "isActive": true,
  "validFrom": "2025-12-01T00:00:00Z",
  "validUntil": "2025-12-31T23:59:59Z"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Customer discount created successfully",
  "data": {
    "id": "new-guid",
    "userId": "customer-guid",
    "userEmail": "customer@example.com",
    "userName": "Jane Smith",
    "name": "Holiday Bonus 10% Off",
    "discountType": "Percentage",
    "discountValue": 10.00,
    "minOrderAmount": 25.00,
    "maxOrderAmount": null,
    "maxUsageCount": 3,
    "usageCount": 0,
    "isActive": true,
    "validFrom": "2025-12-01T00:00:00Z",
    "validUntil": "2025-12-31T23:59:59Z",
    "createdAt": "2025-10-25T12:00:00Z"
  }
}
```

#### 12. Update Customer Discount
```http
PUT /api/admin/CustomerDiscounts/{discountId}
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Updated Holiday Bonus",
  "discountType": "Percentage",
  "discountValue": 15.00,
  "minOrderAmount": 20.00,
  "maxOrderAmount": null,
  "maxUsageCount": 5,
  "isActive": true,
  "validFrom": "2025-12-01T00:00:00Z",
  "validUntil": "2025-12-31T23:59:59Z"
}
```

#### 13. Delete Customer Discount
```http
DELETE /api/admin/CustomerDiscounts/{discountId}
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Customer discount deleted successfully"
}
```

---

## Testing Scenarios

### Scenario 1: New User Makes First Order

1. **User places $35 order**
   - System checks point earning rules
   - Finds "Silver Tier" rule (20-50 range, 15 points)
   - Awards 15 points automatically
   - Creates transaction record

2. **Check balance**
   ```
   GET /api/FidelityPoints/balance
   Response: currentPoints = 15
   ```

3. **View history**
   ```
   GET /api/FidelityPoints/history
   Response: Shows "Earned" transaction with 15 points
   ```

### Scenario 2: Admin Creates VIP Discount

1. **Admin creates 20% discount for specific customer**
   ```
   POST /api/admin/CustomerDiscounts
   Body: userId, name, discountType=Percentage, discountValue=20, minOrderAmount=50
   ```

2. **Customer places $60 order**
   - System finds active customer discount
   - Applies 20% discount ($12 off)
   - Increments usage count
   - Awards points based on original subtotal

3. **Check discount usage**
   ```
   GET /api/admin/CustomerDiscounts/{id}
   Response: usageCount = 1
   ```

### Scenario 3: Points Accumulation Over Multiple Orders

1. **Order 1: $15** → 5 points (Bronze Tier)
2. **Order 2: $40** → 15 points (Silver Tier)  
3. **Order 3: $75** → 30 points (Gold Tier)
4. **Total Balance: 50 points** ($0.50 value)

### Scenario 4: Admin Adjusts Point Rules

1. **Admin updates Gold Tier**
   ```
   PUT /api/admin/PointRules/{goldTierId}
   Body: pointsAwarded = 40 (increased from 30)
   ```

2. **Future orders in 50-100 range earn 40 points**

---

## Validation Rules

### Point Earning Rules
- ✅ Name required (max 100 chars)
- ✅ Min order amount ≥ 0
- ✅ Max order amount > min order amount (if set)
- ✅ Points awarded > 0
- ✅ Priority ≥ 0
- ✅ No overlapping ranges with active rules

### Customer Discounts
- ✅ User ID required and must exist
- ✅ Name required (max 200 chars)
- ✅ Discount type must be "Percentage" or "FixedAmount"
- ✅ Discount value > 0
- ✅ Percentage discounts ≤ 100%
- ✅ Min order amount ≥ 0 (if set)
- ✅ Max order amount > min order amount (if both set)
- ✅ Max usage count > 0 (if set)
- ✅ Valid until > valid from (if both set)

---

## Database Schema

### fidelity_point_balances
- id (uuid, PK)
- user_id (uuid, FK → users, unique)
- current_points (int, default 0)
- total_earned_points (int, default 0)
- total_redeemed_points (int, default 0)
- last_updated (timestamp)
- created_at, updated_at, created_by, updated_by

### fidelity_points_transactions
- id (uuid, PK)
- user_id (uuid, FK → users)
- order_id (uuid, FK → orders, nullable, SET NULL on delete)
- transaction_type (text: Earned, Redeemed, AdminAdjustment, Expired, Refunded)
- points (int, positive for earning, negative for spending)
- order_total (decimal, nullable)
- description (varchar 500)
- expires_at (timestamp, nullable)
- created_at, updated_at, created_by, updated_by

### point_earning_rules
- id (uuid, PK)
- name (varchar 100)
- min_order_amount (decimal 18,2)
- max_order_amount (decimal 18,2, nullable)
- points_awarded (int)
- is_active (bool, default true)
- priority (int, default 0)
- created_at, updated_at, created_by, updated_by

### customer_discount_rules
- id (uuid, PK)
- user_id (uuid, FK → users)
- name (varchar 200)
- discount_type (text: Percentage, FixedAmount)
- discount_value (decimal 18,2)
- min_order_amount (decimal 18,2, nullable)
- max_order_amount (decimal 18,2, nullable)
- max_usage_count (int, nullable)
- usage_count (int, default 0)
- is_active (bool, default true)
- valid_from (timestamp, nullable)
- valid_until (timestamp, nullable)
- created_at, updated_at, created_by, updated_by

### orders (extended)
- fidelity_points_earned (int, default 0)
- fidelity_points_redeemed (int, default 0)
- fidelity_points_discount (decimal, default 0)
- customer_discount_amount (decimal, default 0)
- customer_discount_rule_id (uuid, FK → customer_discount_rules, nullable)

---

## Seeded Data

On application startup, 4 default point earning rules are created:

1. **Bronze Tier**: $0 - $20 → 5 points
2. **Silver Tier**: $20 - $50 → 15 points
3. **Gold Tier**: $50 - $100 → 30 points
4. **Platinum Tier**: $100+ → 60 points

---

## Next Steps

1. **Test with Postman/Swagger**: Use Swagger UI at `http://localhost:5221` to test all endpoints
2. **Integration Testing**: Test order creation with automatic point earning
3. **Frontend Integration** (Phase 4): Display points in account page
4. **Admin UI** (Phase 5): Create management interfaces for rules and discounts

---

## Notes

- All endpoints use `ApiResponse<T>` wrapper for consistent response format
- Authentication required for all endpoints
- Admin role required for admin endpoints
- Point conversion rate: **100 points = $1.00**
- Transactions are atomic with database transactions for consistency
- Points earned based on order subtotal (before discounts)
- Customer discounts automatically applied when order meets criteria
