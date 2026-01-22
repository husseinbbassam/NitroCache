#!/bin/bash

API_URL="http://localhost:5000"

echo "=========================================="
echo "   NitroCache API Demo"
echo "=========================================="
echo ""
echo "Make sure the API is running (dotnet run in NitroCache.Api)"
echo "Press Enter to continue..."
read

echo ""
echo "1️⃣  Testing Health Endpoint..."
curl -s "${API_URL}/health" | jq
sleep 2

echo ""
echo "2️⃣  Getting All Products (First Request - Cache Miss)..."
time curl -s "${API_URL}/api/products" | jq '. | length'
sleep 2

echo ""
echo "3️⃣  Getting All Products Again (Cache Hit - Should be Faster)..."
time curl -s "${API_URL}/api/products" | jq '. | length'
sleep 2

echo ""
echo "4️⃣  Getting Product #1..."
curl -s "${API_URL}/api/products/1" | jq
sleep 2

echo ""
echo "5️⃣  Getting Products in Electronics Category..."
curl -s "${API_URL}/api/products/category/Electronics" | jq '. | length'
sleep 2

echo ""
echo "6️⃣  Invalidating Cache for Product #1..."
curl -s -X DELETE "${API_URL}/api/products/1/cache"
echo "✅ Cache invalidated"
sleep 2

echo ""
echo "7️⃣  Getting Product #1 Again (Cache Miss)..."
curl -s "${API_URL}/api/products/1" | jq
sleep 2

echo ""
echo "8️⃣  Invalidating All Product Caches..."
curl -s -X DELETE "${API_URL}/api/products/cache"
echo "✅ All caches invalidated"
sleep 2

echo ""
echo "9️⃣  Performance Test: 10 Sequential Requests for Product #5..."
echo "First request (cache miss):"
time curl -s "${API_URL}/api/products/5" > /dev/null
echo ""
echo "Next 9 requests (cache hits):"
for i in {1..9}; do
    time curl -s "${API_URL}/api/products/5" > /dev/null
done

echo ""
echo "=========================================="
echo "   Demo Complete!"
echo "=========================================="
echo ""
echo "Key Observations:"
echo "  - First requests are slower (cache miss + DB query)"
echo "  - Subsequent requests are much faster (cache hit)"
echo "  - L1 cache hits should be sub-millisecond"
echo "  - L2 (Redis) cache hits should be 1-5ms"
echo ""
