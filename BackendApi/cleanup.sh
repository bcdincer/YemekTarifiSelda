#!/bin/bash
export FILTER_BRANCH_SQUELCH_WARNING=1
echo 'Secret'larÄ± temizliyorum...'
git filter-branch -f --tree-filter '
if [ -f BackendApi/appsettings.json ]; then
    sed -i.bak \"s/sk-proj-[A-Za-z0-9_-]*//g\" BackendApi/appsettings.json 2>/dev/null || true
    sed -i.bak \"s/hf_[A-Za-z0-9]*//g\" BackendApi/appsettings.json 2>/dev/null || true
    rm -f BackendApi/appsettings.json.bak 2>/dev/null || true
fi
if [ -f BackendApi/appsettings.Development.json ]; then
    rm -f BackendApi/appsettings.Development.json
fi
' --prune-empty --tag-name-filter cat -- --all
echo 'Temizlik tamamlandÄ±!'
