// JWT Token Management
const AUTH_TOKEN_KEY = 'authToken';

// API Base URL - _Layout.cshtml'de tanımlanacak
let API_BASE_URL = window.API_BASE_URL || 'https://localhost:7016';

// Token işlemleri
function getAuthToken() {
    return localStorage.getItem(AUTH_TOKEN_KEY);
}

function setAuthToken(token) {
    if (token) {
        localStorage.setItem(AUTH_TOKEN_KEY, token);
    } else {
        localStorage.removeItem(AUTH_TOKEN_KEY);
    }
}

function removeAuthToken() {
    localStorage.removeItem(AUTH_TOKEN_KEY);
}

function isAuthenticated() {
    const token = getAuthToken();
    if (!token) {
        return false;
    }
    
    // Token varsa ama geçersizse (çok kısa veya boş string), false dön
    if (token.trim().length < 10) {
        return false;
    }
    
    return true;
}

// API çağrıları için header ekle
function getAuthHeaders() {
    const token = getAuthToken();
    const headers = {
        'Content-Type': 'application/json'
    };
    
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    return headers;
}

// Fetch wrapper - otomatik token ekler
async function apiFetch(url, options = {}) {
    const headers = {
        ...getAuthHeaders(),
        ...(options.headers || {})
    };
    
    const response = await fetch(url, {
        ...options,
        headers
    });
    
    // Token expire olduysa logout yap
    if (response.status === 401) {
        removeAuthToken();
        if (window.location.pathname !== '/Account/Login') {
            showToast('Oturumunuz sona erdi. Lütfen tekrar giriş yapın.', 'warning');
            setTimeout(() => {
                window.location.href = '/Account/Login';
            }, 2000);
        }
    }
    
    return response;
}

// Login olduğunda token al
async function fetchAuthToken(userId, userName, email) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userId: userId,
                userName: userName,
                email: email || userName
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            setAuthToken(data.token);
            return data.token;
        } else {
            console.error('Token alınamadı:', await response.text());
            return null;
        }
    } catch (error) {
        console.error('Token alma hatası:', error);
        return null;
    }
}

