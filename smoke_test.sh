#!/bin/bash
for p in /Report /Report/Usage /Report/Dashboard /Report/ResultUsed; do
  code=$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:8123$p")
  echo "$p -> $code"
done
echo "--- admin body markers ---"
curl -s "http://localhost:8123/Report/ResultUsed" | grep -o 'SysAdmin[^<]*\|MST được xem[^<]*\|K1_TongSo\|TINVOICODE[^<]*' | head
echo "--- nnt_a body markers ---"
curl -s "http://localhost:8123/Report/ResultUsed?userCode=nnt_a" | grep -o 'Phân quyền theo MST[^<]*\|MST được xem[^<]*' | head