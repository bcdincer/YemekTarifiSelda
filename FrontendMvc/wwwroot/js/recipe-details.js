// Recipe Details JavaScript
// This file contains all JavaScript functionality for the recipe details page

// Wait for DOM to be ready
(function() {
    'use strict';

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
        // Check if recipeData is available
        if (!window.recipeData) {
            console.warn('recipeData not found. Make sure window.recipeData is set in Details.cshtml');
            return;
        }

        // Initialize all components
        initImageNavigation();
        initImageLightbox();
        initIngredientsViewer();
        initStepsViewer();
        initServingsCalculator();
        initRatingSystem();
        initLikeSystem();
        initShareModal();
        initCollectionModal();
        initCommentSystem();
        initTitleButtonsLayout();
        initPrintButton();
        
        // Load similar recipes and author recipes
        loadSimilarRecipes();
        if (window.recipeData.authorId) {
            loadAuthorRecipes(window.recipeData.authorId);
        }
        
        // Initialize equalize heights
        setTimeout(() => {
            equalizeHeights();
        }, 300);
        
        setTimeout(() => {
            equalizeHeights();
        }, 800);
        
        setTimeout(() => {
            equalizeHeights();
        }, 1500);
    }

    // Image Navigation
    function initImageNavigation() {
        if (!window.recipeImages || !window.recipeImages.length) {
            return;
        }

        // Navigate image function
        window.navigateImage = function(direction) {
            if (!window.recipeImages || window.recipeImages.length <= 1) {
                return;
            }

            const totalImages = window.recipeImages.length;
            
            // Calculate new index with circular navigation
            if (direction > 0) {
                // Next image
                window.currentImageIndex = (window.currentImageIndex + 1) % totalImages;
            } else {
                // Previous image
                window.currentImageIndex = (window.currentImageIndex - 1 + totalImages) % totalImages;
            }

            // Update main image
            const recipeImage = document.getElementById('recipeImage');
            const imageCounter = document.getElementById('imageCounter');
            
            if (recipeImage && window.recipeImages[window.currentImageIndex]) {
                recipeImage.src = window.recipeImages[window.currentImageIndex].url;
                
                // Update counter
                if (imageCounter) {
                    imageCounter.textContent = `${window.currentImageIndex + 1}/${totalImages}`;
                }
            }
        };

        // Keyboard navigation
        document.addEventListener('keydown', function(e) {
            if (e.key === 'ArrowLeft') {
                window.navigateImage(-1);
            } else if (e.key === 'ArrowRight') {
                window.navigateImage(1);
            }
        });

        // Update image counter on load
        const imageCounter = document.getElementById('imageCounter');
        if (imageCounter && window.recipeImages) {
            imageCounter.textContent = `${(window.currentImageIndex || 0) + 1}/${window.recipeImages.length}`;
        }
        
        // Event listeners for navigation buttons
        const prevImageBtn = document.getElementById('prevImageBtn');
        const nextImageBtn = document.getElementById('nextImageBtn');
        
        if (prevImageBtn) {
            prevImageBtn.addEventListener('click', function(e) {
                e.stopPropagation();
                window.navigateImage(-1);
            });
        }
        
        if (nextImageBtn) {
            nextImageBtn.addEventListener('click', function(e) {
                e.stopPropagation();
                window.navigateImage(1);
            });
        }
    }

    // Quill Editor for Ingredients
    function initIngredientsViewer() {
        if (typeof Quill === 'undefined') {
            console.warn('Quill library not loaded, using plain text display');
            const ingredientsList = document.getElementById('ingredientsList');
            if (ingredientsList) {
                ingredientsList.classList.remove('d-none');
            }
            return;
        }

        try {
            const ingredientsViewer = new Quill('#ingredientsViewer', {
                modules: {
                    toolbar: false
                },
                readOnly: true,
                theme: 'snow'
            });

            const qlContainer = document.querySelector('#ingredientsViewer .ql-container');
            const qlEditor = document.querySelector('#ingredientsViewer .ql-editor');
            
            if (qlContainer) {
                qlContainer.style.minHeight = '400px';
                qlContainer.style.overflowY = 'auto';
            }
            if (qlEditor) {
                qlEditor.style.minHeight = '380px';
                qlEditor.setAttribute('spellcheck', 'false');
            }

            // Dark mode application
            const applyDarkModeStyles = () => {
                const isDarkMode = document.documentElement.getAttribute('data-theme') === 'dark';
                if (qlEditor) {
                    if (isDarkMode) {
                        qlEditor.style.backgroundColor = '#2d2d3f';
                        qlEditor.style.color = '#e0e0e0';
                    } else {
                        qlEditor.style.backgroundColor = '#f5f5f5';
                        qlEditor.style.color = '#333';
                    }
                }
                const container = document.querySelector('#ingredientsViewer .ql-container');
                if (container) {
                    if (isDarkMode) {
                        container.style.borderColor = '#3d3d4f';
                    } else {
                        container.style.borderColor = '#ccc';
                    }
                }
            };

            applyDarkModeStyles();
            const themeObserver = new MutationObserver(() => applyDarkModeStyles());
            themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

            // Load ingredients content
            const ingredientsText = window.recipeData.ingredients || '';
            
            const convertPlainTextToHtml = (plainText) => {
                if (!plainText) return '<p><br></p>';
                const lines = plainText.split('\n').filter(line => line.trim());
                let html = '';
                let inList = false;
                
                lines.forEach(line => {
                    const trimmed = line.trim();
                    if (trimmed.startsWith('• ') || trimmed.startsWith('- ')) {
                        if (!inList) {
                            html += '<ul>';
                            inList = true;
                        }
                        const content = trimmed.replace(/^[•\-]\s*/, '');
                        html += `<li>${content}</li>`;
                    } else {
                        if (inList) {
                            html += '</ul>';
                            inList = false;
                        }
                        html += `<p>${trimmed}</p>`;
                    }
                });
                if (inList) {
                    html += '</ul>';
                }
                return html || '<p><br></p>';
            };

            let ingredientsHtml = ingredientsText;
            if (!ingredientsHtml || (typeof ingredientsHtml === 'string' && !ingredientsHtml.includes('<') && !ingredientsHtml.includes('>'))) {
                ingredientsHtml = convertPlainTextToHtml(ingredientsText);
            }
            
            ingredientsViewer.root.innerHTML = ingredientsHtml;
            
            setTimeout(() => {
                if (typeof equalizeHeights === 'function') {
                    equalizeHeights();
                }
            }, 200);
        } catch (err) {
            console.error('Error initializing Quill viewer:', err);
            const ingredientsList = document.getElementById('ingredientsList');
            if (ingredientsList) {
                ingredientsList.classList.remove('d-none');
            }
            
            setTimeout(() => {
                if (typeof equalizeHeights === 'function') {
                    equalizeHeights();
                }
            }, 200);
        }
    }

    // Quill Editor for Steps
    function initStepsViewer() {
        if (typeof Quill === 'undefined') {
            console.warn('Quill library not loaded, using plain text display');
            const stepsList = document.getElementById('stepsList');
            if (stepsList) {
                stepsList.classList.remove('d-none');
            }
            return;
        }

        try {
            const stepsViewer = new Quill('#stepsViewer', {
                modules: {
                    toolbar: false
                },
                readOnly: true,
                theme: 'snow'
            });

            const qlContainer = document.querySelector('#stepsViewer .ql-container');
            const qlEditor = document.querySelector('#stepsViewer .ql-editor');
            
            if (qlContainer) {
                qlContainer.style.minHeight = '400px';
                qlContainer.style.overflowY = 'auto';
            }
            if (qlEditor) {
                qlEditor.style.minHeight = '380px';
                qlEditor.setAttribute('spellcheck', 'false');
            }

            // Dark mode application
            const applyDarkModeStyles = () => {
                const isDarkMode = document.documentElement.getAttribute('data-theme') === 'dark';
                if (qlEditor) {
                    if (isDarkMode) {
                        qlEditor.style.backgroundColor = '#2d2d3f';
                        qlEditor.style.color = '#e0e0e0';
                    } else {
                        qlEditor.style.backgroundColor = '#f5f5f5';
                        qlEditor.style.color = '#333';
                    }
                }
                const container = document.querySelector('#stepsViewer .ql-container');
                if (container) {
                    if (isDarkMode) {
                        container.style.borderColor = '#3d3d4f';
                    } else {
                        container.style.borderColor = '#ccc';
                    }
                }
            };

            applyDarkModeStyles();
            const themeObserver = new MutationObserver(() => applyDarkModeStyles());
            themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

            // Load steps content
            const stepsText = window.recipeData.steps || '';
            
            const convertPlainTextToHtml = (plainText) => {
                if (!plainText) return '<p><br></p>';
                const lines = plainText.split('\n').filter(line => line.trim());
                let html = '';
                let inList = false;
                let listType = null;
                
                lines.forEach(line => {
                    const trimmed = line.trim();
                    if (/^\d+\.\s/.test(trimmed)) {
                        if (!inList || listType !== 'ol') {
                            if (inList && listType === 'ul') {
                                html += '</ul>';
                            }
                            html += '<ol>';
                            inList = true;
                            listType = 'ol';
                        }
                        const content = trimmed.replace(/^\d+\.\s*/, '');
                        html += `<li>${content}</li>`;
                    } else if (trimmed.startsWith('• ') || trimmed.startsWith('- ')) {
                        if (!inList || listType !== 'ul') {
                            if (inList && listType === 'ol') {
                                html += '</ol>';
                            }
                            html += '<ul>';
                            inList = true;
                            listType = 'ul';
                        }
                        const content = trimmed.replace(/^[•\-]\s*/, '');
                        html += `<li>${content}</li>`;
                    } else {
                        if (inList) {
                            html += listType === 'ol' ? '</ol>' : '</ul>';
                            inList = false;
                            listType = null;
                        }
                        html += `<p>${trimmed}</p>`;
                    }
                });
                if (inList) {
                    html += listType === 'ol' ? '</ol>' : '</ul>';
                }
                return html || '<p><br></p>';
            };
            
            let stepsHtml = stepsText;
            if (!stepsHtml || (typeof stepsHtml === 'string' && !stepsHtml.includes('<') && !stepsHtml.includes('>'))) {
                stepsHtml = convertPlainTextToHtml(stepsText);
            }
            
            stepsViewer.root.innerHTML = stepsHtml;
            
            setTimeout(() => {
                if (typeof equalizeHeights === 'function') {
                    equalizeHeights();
                }
            }, 200);
        } catch (err) {
            console.error('Error initializing Quill steps viewer:', err);
            const stepsList = document.getElementById('stepsList');
            if (stepsList) {
                stepsList.classList.remove('d-none');
            }
            
            setTimeout(() => {
                if (typeof equalizeHeights === 'function') {
                    equalizeHeights();
                }
            }, 200);
        }
    }

    // Debounce function
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
    
    // Equalize heights
    let isEqualizing = false;
    let lastEqualizedHeight = 0;
    
    window.equalizeHeights = function() {
        if (isEqualizing) return;
        
        const ingredientsContainer = document.querySelector('.numbered-ingredients-container');
        const stepsContainer = document.querySelector('.numbered-steps-container');
        
        if (!ingredientsContainer || !stepsContainer) return;
        
        isEqualizing = true;
        
        try {
            const minHeight = 400;
            const stepsHeight = stepsContainer.offsetHeight;
            const actualStepsHeight = stepsHeight > 50 ? stepsHeight : stepsContainer.scrollHeight;
            const targetHeight = Math.max(minHeight, actualStepsHeight);
            
            if (Math.abs(lastEqualizedHeight - targetHeight) < 2) {
                isEqualizing = false;
                return;
            }
            
            lastEqualizedHeight = targetHeight;
            
            ingredientsContainer.style.height = targetHeight + 'px';
            ingredientsContainer.style.minHeight = targetHeight + 'px';
            ingredientsContainer.style.maxHeight = targetHeight + 'px';
            stepsContainer.style.height = targetHeight + 'px';
            stepsContainer.style.minHeight = targetHeight + 'px';
            stepsContainer.style.maxHeight = targetHeight + 'px';
            
            const ingredientsQlContainer = document.querySelector('#ingredientsViewer .ql-container');
            if (ingredientsQlContainer) {
                const containerPadding = 40;
                const editorHeight = targetHeight - containerPadding;
                ingredientsQlContainer.style.height = editorHeight + 'px';
                ingredientsQlContainer.style.minHeight = editorHeight + 'px';
            }
            
            const ingredientsQlEditor = document.querySelector('#ingredientsViewer .ql-editor');
            if (ingredientsQlEditor) {
                const containerPadding = 40;
                const editorContentHeight = targetHeight - containerPadding - 20;
                ingredientsQlEditor.style.minHeight = editorContentHeight + 'px';
            }
            
            const ingredientsList = document.getElementById('ingredientsList');
            if (ingredientsList && !ingredientsList.classList.contains('d-none')) {
                const containerPadding = 40;
                ingredientsList.style.minHeight = (targetHeight - containerPadding) + 'px';
            }
            
            const stepsQlContainer = document.querySelector('#stepsViewer .ql-container');
            if (stepsQlContainer) {
                const containerPadding = 40;
                const editorHeight = targetHeight - containerPadding;
                stepsQlContainer.style.height = editorHeight + 'px';
                stepsQlContainer.style.minHeight = editorHeight + 'px';
            }
            
            const stepsQlEditor = document.querySelector('#stepsViewer .ql-editor');
            if (stepsQlEditor) {
                const containerPadding = 40;
                const editorContentHeight = targetHeight - containerPadding - 20;
                stepsQlEditor.style.minHeight = editorContentHeight + 'px';
            }
            
            const stepsList = document.getElementById('stepsList');
            if (stepsList && !stepsList.classList.contains('d-none')) {
                const containerPadding = 40;
                stepsList.style.minHeight = (targetHeight - containerPadding) + 'px';
            }
        } finally {
            setTimeout(() => {
                isEqualizing = false;
            }, 150);
        }
    }
    
    const debouncedEqualizeHeights = debounce(window.equalizeHeights, 300);
    
    window.addEventListener('resize', debouncedEqualizeHeights);
    
    if (window.ResizeObserver) {
        const stepsContainer = document.querySelector('.numbered-steps-container');
        if (stepsContainer) {
            const resizeObserver = new ResizeObserver(() => {
                debouncedEqualizeHeights();
            });
            resizeObserver.observe(stepsContainer);
        }
    }

    // Print Button
    function initPrintButton() {
        const printBtn = document.getElementById('printBtn');
        if (printBtn) {
            printBtn.addEventListener('click', function() {
                window.print();
            });
        }
    }

    // Title and Buttons Layout
    function initTitleButtonsLayout() {
        function adjustTitleButtonsLayout() {
            const titleContainer = document.getElementById('titleContainer');
            const buttonsContainer = document.getElementById('buttonsContainer');
            const titleButtonsContainer = document.getElementById('titleButtonsContainer');
            const recipeTitle = document.getElementById('recipeTitle');
            
            if (!titleContainer || !buttonsContainer || !titleButtonsContainer || !recipeTitle) {
                return;
            }
            
            // Check if title wraps to more than 1.5 lines
            const titleHeight = recipeTitle.scrollHeight;
            const titleLineHeight = parseFloat(window.getComputedStyle(recipeTitle).lineHeight);
            const maxHeight = titleLineHeight * 1.5;
            
            if (titleHeight > maxHeight) {
                // Move buttons below title
                titleButtonsContainer.style.flexDirection = 'column';
                titleButtonsContainer.style.alignItems = 'flex-start';
                buttonsContainer.style.marginTop = '10px';
            } else {
                // Keep buttons next to title
                titleButtonsContainer.style.flexDirection = 'row';
                titleButtonsContainer.style.alignItems = 'center';
                buttonsContainer.style.marginTop = '0';
            }
        }
        
        adjustTitleButtonsLayout();
        window.addEventListener('resize', adjustTitleButtonsLayout);
    }

    // Helper functions
    const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7016';
    
    function isAuthenticated() {
        return typeof window.isAuthenticated === 'function' ? window.isAuthenticated() : false;
    }
    
    function apiFetch(url, options = {}) {
        if (typeof window.apiFetch === 'function') {
            return window.apiFetch(url, options);
        }
        return fetch(url, options);
    }
    
    function showToast(message, type = 'info') {
        if (typeof window.showToast === 'function') {
            window.showToast(message, type);
        } else {
            console.log(`[${type.toUpperCase()}] ${message}`);
        }
    }
    
    function requireLogin(message) {
        const currentUrl = window.location.pathname + window.location.search;
        const returnUrl = encodeURIComponent(currentUrl);
        const loginUrl = `/Account/Login?returnUrl=${returnUrl}`;
        
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'warning',
                title: 'Giriş Gerekli',
                text: message,
                showCancelButton: true,
                confirmButtonText: 'Giriş Yap',
                cancelButtonText: 'İptal',
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                reverseButtons: true
            }).then((result) => {
                if (result.isConfirmed) {
                    window.location.href = loginUrl;
                }
            });
        } else {
            if (confirm(message + '\n\nGiriş sayfasına yönlendirilmek ister misiniz?')) {
                window.location.href = loginUrl;
            }
        }
        return false;
    }

    // Servings Calculator
    function initServingsCalculator() {
        const originalServings = window.recipeData.servings || 1;
        let originalIngredients = [];
        
        function initIngredients() {
            originalIngredients = Array.from(document.querySelectorAll('#ingredientsList li')).map(li => ({
                element: li,
                text: li.getAttribute('data-original') || li.textContent.trim()
            }));
        }
        
        window.calculateServings = async function() {
            const input = document.getElementById('servingsMultiplier');
            const calculateBtn = document.getElementById('calculateBtn');
            const newServings = parseFloat(input?.value);
            
            if (!newServings || newServings < 1 || newServings > 20) {
                showToast('Lütfen geçerli bir kişi sayısı girin (1-20 arası)', 'warning');
                input?.focus();
                return;
            }
            
            if (newServings === originalServings) {
                if (typeof Swal !== 'undefined') {
                    const result = await Swal.fire({
                        icon: 'question',
                        title: 'Kişi Sayısı Değişmedi',
                        text: 'Kişi sayısı değişmedi. Yine de hesaplamak istiyor musunuz?',
                        showCancelButton: true,
                        confirmButtonText: 'Evet, Hesapla',
                        cancelButtonText: 'İptal',
                        confirmButtonColor: '#3085d6',
                        cancelButtonColor: '#d33',
                        reverseButtons: true
                    });
                    if (!result.isConfirmed) return;
                }
            }
            
            if (!originalIngredients.length) initIngredients();
            
            const originalBtnText = calculateBtn?.innerHTML;
            if (calculateBtn) {
                calculateBtn.disabled = true;
                calculateBtn.innerHTML = '<i class="ti-reload" style="animation: spin 1s linear infinite; margin-right: 4px; display: inline-block;"></i> Hesaplanıyor...';
            }
            if (input) input.disabled = true;
            
            try {
                const useAi = document.getElementById('useAiToggle')?.checked || false;
                const multiplier = newServings / originalServings;
                
                if (useAi) {
                    try {
                        const ingredients = originalIngredients.map(ing => ing.text);
                        const response = await fetch(`${apiBaseUrl}/api/recipes/adjust-ingredients`, {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({
                                ingredients: ingredients,
                                originalServings: originalServings,
                                newServings: newServings
                            })
                        });
                        
                        if (response.ok) {
                            const data = await response.json();
                            if (data.ingredients && data.ingredients.length === originalIngredients.length) {
                                originalIngredients.forEach(({ element }, index) => {
                                    element.textContent = data.ingredients[index];
                                });
                                showToast('Malzemeler AI ile başarıyla güncellendi!', 'success');
                                return;
                            }
                        }
                    } catch (error) {
                        console.error('AI adjustment error:', error);
                    }
                }
                
                // Fallback: Mathematical calculation
                originalIngredients.forEach(({ element, text }) => {
                    let updatedText = text;
                    const fractionMap = {
                        'yarım': 0.5, 'yarı': 0.5, 'çeyrek': 0.25,
                        'üçte bir': 1/3, 'üçte iki': 2/3,
                        'dörtte bir': 0.25, 'dörtte üç': 0.75
                    };
                    
                    for (const [fraction, value] of Object.entries(fractionMap)) {
                        const regex = new RegExp(`(${fraction})\\s+([a-zA-ZğüşıöçĞÜŞİÖÇ\\s]+)`, 'gi');
                        updatedText = updatedText.replace(regex, (match, frac, unit) => {
                            const newValue = (value * multiplier).toFixed(2);
                            if (Math.abs(newValue - 1) < 0.01) return `1 ${unit.trim()}`;
                            if (Math.abs(newValue - 0.5) < 0.01) return `yarım ${unit.trim()}`;
                            return `${newValue} ${unit.trim()}`;
                        });
                    }
                    
                    updatedText = updatedText.replace(/(\d+\.?\d*)\s+([a-zA-ZğüşıöçĞÜŞİÖÇ\s]+?)(?=\s|$|,|\.)/g, (match, num, unit) => {
                        const numValue = parseFloat(num);
                        const newNum = numValue * multiplier;
                        if (newNum % 1 === 0) return `${Math.round(newNum)} ${unit.trim()}`;
                        if (newNum < 1) return `${newNum.toFixed(2).replace(/\.?0+$/, '')} ${unit.trim()}`;
                        return `${newNum.toFixed(1).replace(/\.?0+$/, '')} ${unit.trim()}`;
                    });
                    
                    element.textContent = updatedText;
                });
                
                showToast('Malzemeler başarıyla güncellendi!', 'success');
            } catch (error) {
                console.error('Error adjusting servings:', error);
                showToast('Malzemeler hesaplanırken bir hata oluştu.', 'error');
            } finally {
                if (calculateBtn) {
                    calculateBtn.disabled = false;
                    calculateBtn.innerHTML = originalBtnText || 'Hesapla';
                }
                if (input) input.disabled = false;
            }
        };
        
        window.resetServings = function() {
            if (!originalIngredients.length) initIngredients();
            const input = document.getElementById('servingsMultiplier');
            if (input) input.value = originalServings;
            originalIngredients.forEach(({ element, text }) => {
                element.textContent = text;
            });
        };
        
        // Event listeners
        const calculateBtn = document.getElementById('calculateBtn');
        const resetBtn = document.getElementById('resetServingsBtn');
        
        if (calculateBtn) {
            calculateBtn.addEventListener('click', window.calculateServings);
        }
        if (resetBtn) {
            resetBtn.addEventListener('click', window.resetServings);
        }
        
        initIngredients();
    }

    // Rating System
    function initRatingSystem() {
        const recipeId = window.recipeData.recipeId;
        let currentUserRating = null;
        
        async function loadUserRating() {
            if (!isAuthenticated()) {
                currentUserRating = 0;
                return;
            }
            
            try {
                const response = await apiFetch(`${apiBaseUrl}/api/recipes/${recipeId}/rating/user`);
                if (response.ok) {
                    const data = await response.json();
                    if (data.rating && data.rating > 0) {
                        currentUserRating = data.rating;
                        updateUserRatingDisplay(data.rating);
                    } else {
                        currentUserRating = 0;
                        updateUserRatingDisplay(0);
                    }
                }
            } catch (error) {
                console.error('Rating yüklenirken hata:', error);
                currentUserRating = 0;
                updateUserRatingDisplay(0);
            }
        }
        
        function updateUserRatingDisplay(rating) {
            const userRatingDisplay = document.getElementById('userRatingDisplay');
            if (userRatingDisplay) {
                if (rating && rating > 0) {
                    userRatingDisplay.innerHTML = `Puanınız : ${rating}`;
                    userRatingDisplay.style.display = 'inline';
                } else {
                    userRatingDisplay.style.display = 'none';
                }
            }
        }
        
        function updateStarDisplay(rating, isHover = false) {
            let visualRating = 0;
            if (rating && rating > 0) {
                visualRating = rating * 2; // 1-5 -> 2-10
            }
            
            const starRatingContainer = document.querySelector('.star-rating');
            if (!starRatingContainer) return;
            
            let stars = starRatingContainer.querySelectorAll('.star-icon');
            if (stars.length === 0) {
                starRatingContainer.innerHTML = '';
                for (let i = 0; i < 5; i++) {
                    const star = document.createElement('i');
                    star.className = 'ti-star star-icon';
                    star.setAttribute('data-rating', i + 1);
                    star.style.cssText = 'font-size: 20px; cursor: pointer; color: #ddd; transition: all 0.2s; line-height: 1; display: inline-block;';
                    starRatingContainer.appendChild(star);
                }
                stars = starRatingContainer.querySelectorAll('.star-icon');
            }
            
            stars.forEach((star, index) => {
                const starValue = (index + 1) * 2;
                const starStart = index * 2;
                
                star.classList.remove('rated', 'active');
                star.style.setProperty('color', '#ddd', 'important');
                
                if (visualRating >= starValue) {
                    star.classList.add('rated', 'active');
                    star.style.setProperty('color', '#ffc107', 'important');
                } else if (visualRating > starStart && visualRating < starValue) {
                    const halfStarContainer = document.createElement('div');
                    halfStarContainer.className = 'half-star-container';
                    halfStarContainer.setAttribute('data-rating', index + 1);
                    halfStarContainer.style.cssText = 'position: relative; display: inline-block; width: 20px; height: 20px; line-height: 1; cursor: pointer;';
                    
                    const emptyStar = document.createElement('i');
                    emptyStar.className = 'ti-star';
                    emptyStar.style.cssText = 'font-size: 20px; position: absolute; left: 0; top: 0; color: #ddd; z-index: 1; width: 20px; height: 20px;';
                    
                    const fractionalPart = (visualRating - starStart) / (starValue - starStart);
                    const fillPercentage = fractionalPart * 100;
                    
                    const filledStarWrapper = document.createElement('div');
                    filledStarWrapper.style.cssText = `position: absolute; left: 0; top: 0; width: ${fillPercentage}%; height: 20px; overflow: hidden; z-index: 2;`;
                    
                    const filledStar = document.createElement('i');
                    filledStar.className = 'ti-star';
                    filledStar.style.cssText = 'font-size: 20px; position: absolute; left: 0; top: 0; color: #ffc107; width: 20px; height: 20px; display: block;';
                    
                    filledStarWrapper.appendChild(filledStar);
                    halfStarContainer.appendChild(emptyStar);
                    halfStarContainer.appendChild(filledStarWrapper);
                    star.replaceWith(halfStarContainer);
                }
            });
        }
        
        function attachStarEventListeners() {
            const starRatingContainer = document.querySelector('.star-rating');
            if (!starRatingContainer) return;
            
            starRatingContainer.addEventListener('click', async function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                const target = e.target.closest('.star-icon, .half-star-container');
                if (!target) return false;
                
                let starIndex = -1;
                if (target.classList.contains('star-icon')) {
                    starIndex = parseInt(target.getAttribute('data-rating')) - 1;
                } else if (target.classList.contains('half-star-container')) {
                    starIndex = parseInt(target.getAttribute('data-rating')) - 1;
                }
                
                if (starIndex >= 0) {
                    if (!isAuthenticated()) {
                        requireLogin('Puan vermek için giriş yapmanız gerekiyor.');
                        return false;
                    }
                    
                    const clickRating = starIndex + 1;
                    
                    let confirmed = false;
                    if (typeof Swal !== 'undefined') {
                        const result = await Swal.fire({
                            icon: 'question',
                            title: 'Puan Vermek İstediğinize Emin misiniz?',
                            html: `Bu tarife <strong>${clickRating} yıldız</strong> vermek istediğinize emin misiniz?`,
                            showCancelButton: true,
                            confirmButtonText: 'Evet, Puan Ver',
                            cancelButtonText: 'İptal',
                            confirmButtonColor: '#3085d6',
                            cancelButtonColor: '#d33',
                            reverseButtons: true
                        });
                        confirmed = result.isConfirmed;
                    } else {
                        confirmed = confirm(`Bu tarife ${clickRating} yıldız vermek istediğinizden emin misiniz?`);
                    }
                    
                    if (!confirmed) {
                        const initialRating = parseFloat(starRatingContainer.getAttribute('data-initial-rating')) || 0;
                        updateStarDisplay(initialRating, false);
                        return;
                    }
                    
                    try {
                        const response = await apiFetch(`${apiBaseUrl}/api/recipes/${recipeId}/rate?rating=${clickRating}`, {
                            method: 'POST'
                        });
                        
                        if (response.ok) {
                            const data = await response.json();
                            currentUserRating = clickRating;
                            updateUserRatingDisplay(clickRating);
                            await updateRatingStats();
                        } else {
                            showToast('Puan verilirken bir hata oluştu.', 'error');
                            const initialRating = parseFloat(starRatingContainer.getAttribute('data-initial-rating')) || 0;
                            updateStarDisplay(initialRating, false);
                        }
                    } catch (error) {
                        console.error('Puan verilirken hata:', error);
                        showToast('Puan verilirken bir hata oluştu.', 'error');
                        const initialRating = parseFloat(starRatingContainer.getAttribute('data-initial-rating')) || 0;
                        updateStarDisplay(initialRating, false);
                    }
                }
            });
        }
        
        async function updateRatingStats() {
            try {
                const response = await fetch(`${apiBaseUrl}/api/recipes/${recipeId}/rating`);
                if (response.ok) {
                    const data = await response.json();
                    const ratingDisplay = document.getElementById('ratingStats');
                    if (ratingDisplay) {
                        if (data.averageRating !== null && data.averageRating !== undefined) {
                            ratingDisplay.innerHTML = `Puan : ${data.averageRating.toFixed(1)} <span class="c-grey-500">(${data.ratingCount} Kişi Puan Verdi)</span>`;
                            
                            if (currentUserRating && currentUserRating > 0) {
                                updateUserRatingDisplay(currentUserRating);
                            }
                            
                            const starRatingContainer = document.querySelector('.star-rating');
                            if (starRatingContainer) {
                                starRatingContainer.setAttribute('data-initial-rating', data.averageRating);
                                updateStarDisplay(data.averageRating, false);
                            }
                        } else {
                            ratingDisplay.innerHTML = `<span class="c-grey-500">-</span>`;
                        }
                    }
                }
            } catch (error) {
                console.error('Rating istatistikleri güncellenirken hata:', error);
            }
        }
        
        // Initialize
        loadUserRating();
        attachStarEventListeners();
        
        const starRatingContainer = document.querySelector('.star-rating');
        if (starRatingContainer) {
            const initialRatingStr = starRatingContainer.getAttribute('data-initial-rating');
            const initialRating = parseFloat(initialRatingStr) || 0;
            if (initialRating > 0) {
                updateStarDisplay(initialRating, false);
            }
        }
    }

    // Like System
    function initLikeSystem() {
        const recipeId = window.recipeData.recipeId;
        let isLiked = false;
        
        async function loadLikeStatus() {
            if (!isAuthenticated()) {
                isLiked = false;
                updateLikeButton();
                return;
            }
            
            try {
                const response = await apiFetch(`${apiBaseUrl}/api/recipes/${recipeId}/like`);
                if (response.ok) {
                    const data = await response.json();
                    isLiked = data.isLiked;
                    updateLikeButton();
                }
            } catch (error) {
                console.error('Like durumu yüklenirken hata:', error);
            }
        }
        
        function updateLikeButton() {
            const likeIconStat = document.getElementById('likeIconStat');
            const likeButtonStat = document.getElementById('likeButtonStat');
            if (likeIconStat) {
                if (isLiked) {
                    likeIconStat.classList.add('liked');
                    likeIconStat.style.setProperty('color', '#dc3545', 'important');
                    likeIconStat.style.setProperty('font-weight', '900', 'important');
                } else {
                    likeIconStat.classList.remove('liked');
                    likeIconStat.style.setProperty('color', '#999', 'important');
                    likeIconStat.style.setProperty('font-weight', 'normal', 'important');
                }
            }
        }
        
        async function toggleLike(e) {
            if (e) {
                e.preventDefault();
                e.stopPropagation();
            }
            
            if (!isAuthenticated()) {
                requireLogin('Beğenmek için giriş yapmanız gerekiyor.');
                return false;
            }
            
            try {
                const response = await apiFetch(`${apiBaseUrl}/api/recipes/${recipeId}/like`, {
                    method: 'POST'
                });
                
                if (response.ok) {
                    const data = await response.json();
                    isLiked = data.isLiked;
                    updateLikeButton();
                    
                    const likeCountElement = document.getElementById('likeCount');
                    if (likeCountElement) {
                        likeCountElement.textContent = data.likeCount;
                    }
                } else {
                    showToast('Beğeni işlemi sırasında bir hata oluştu.', 'error');
                }
            } catch (error) {
                console.error('Beğeni işlemi sırasında hata:', error);
                showToast('Beğeni işlemi sırasında bir hata oluştu.', 'error');
            }
        }
        
        // Event listeners
        const likeButtonStat = document.getElementById('likeButtonStat');
        if (likeButtonStat) {
            likeButtonStat.addEventListener('click', toggleLike);
        }
        
        const recipeImage = document.getElementById('recipeImage');
        if (recipeImage) {
            recipeImage.addEventListener('dblclick', toggleLike);
        }
        
        // Make toggleLike globally available
        window.toggleLike = toggleLike;
        
        loadLikeStatus();
    }

    // Share Modal
    function initShareModal() {
        const recipeUrl = window.recipeData.recipeUrl;
        const recipeTitle = window.recipeData.recipeTitle;
        const recipeDescription = window.recipeData.recipeDescription;
        
        window.openShareModal = function() {
            const modal = document.getElementById('shareModal');
            if (modal) {
                modal.style.display = 'block';
                modal.classList.add('show');
                generateQRCode();
            }
        };
        
        window.closeShareModal = function() {
            const modal = document.getElementById('shareModal');
            if (modal) {
                modal.style.display = 'none';
                modal.classList.remove('show');
            }
        };
        
        function generateQRCode() {
            const qrContainer = document.getElementById('qrcode');
            if (!qrContainer) return;
            
            qrContainer.innerHTML = '';
            
            if (typeof QRCode !== 'undefined') {
                const canvas = document.createElement('canvas');
                QRCode.toCanvas(canvas, recipeUrl, {
                    width: 200,
                    margin: 2,
                    color: { dark: '#000000', light: '#FFFFFF' }
                }, function (error) {
                    if (error) {
                        console.error('QR Code generation error:', error);
                        qrContainer.innerHTML = '<p class="c-grey-600">QR kod oluşturulamadı</p>';
                    } else {
                        qrContainer.appendChild(canvas);
                    }
                });
            } else {
                qrContainer.innerHTML = '<p class="c-grey-600">QR kod yükleniyor...</p>';
                setTimeout(() => {
                    if (typeof QRCode !== 'undefined') {
                        generateQRCode();
                    }
                }, 500);
            }
        }
        
        window.shareToWhatsApp = function() {
            const text = encodeURIComponent(`${recipeTitle}\n\n${recipeUrl}`);
            const whatsappUrl = `https://wa.me/?text=${text}`;
            window.open(whatsappUrl, '_blank');
        };
        
        window.shareToTelegram = function() {
            const text = encodeURIComponent(`${recipeTitle}\n\n${recipeUrl}`);
            const telegramUrl = `https://t.me/share/url?url=${encodeURIComponent(recipeUrl)}&text=${text}`;
            window.open(telegramUrl, '_blank');
        };
        
        window.shareToFacebook = function() {
            const facebookUrl = `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(recipeUrl)}`;
            window.open(facebookUrl, '_blank', 'width=600,height=400');
        };
        
        window.shareToTwitter = function() {
            const text = encodeURIComponent(`${recipeTitle} - ${recipeDescription}`);
            const twitterUrl = `https://twitter.com/intent/tweet?url=${encodeURIComponent(recipeUrl)}&text=${text}`;
            window.open(twitterUrl, '_blank', 'width=600,height=400');
        };
        
        window.copyRecipeLink = function() {
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(recipeUrl).then(() => {
                    showToast('Tarif linki kopyalandı!', 'success');
                    closeShareModal();
                }).catch(() => {
                    fallbackCopyTextToClipboard(recipeUrl);
                });
            } else {
                fallbackCopyTextToClipboard(recipeUrl);
            }
        };
        
        function fallbackCopyTextToClipboard(text) {
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.top = '0';
            textArea.style.left = '0';
            textArea.style.width = '2em';
            textArea.style.height = '2em';
            textArea.style.padding = '0';
            textArea.style.border = 'none';
            textArea.style.outline = 'none';
            textArea.style.boxShadow = 'none';
            textArea.style.background = 'transparent';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            try {
                const successful = document.execCommand('copy');
                if (successful) {
                    showToast('Tarif linki kopyalandı!', 'success');
                    closeShareModal();
                } else {
                    prompt('Linki kopyalamak için Ctrl+C kullanın:', text);
                }
            } catch (err) {
                prompt('Linki kopyalamak için Ctrl+C kullanın:', text);
            }
            document.body.removeChild(textArea);
        }
        
        // Close modal when clicking outside
        window.addEventListener('click', function(event) {
            const modal = document.getElementById('shareModal');
            if (event.target === modal) {
                closeShareModal();
            }
        });
        
        // Event listeners
        const shareBtn = document.getElementById('shareBtn');
        const closeShareModalBtn = document.getElementById('closeShareModalBtn');
        const shareWhatsAppBtn = document.getElementById('shareWhatsAppBtn');
        const shareTelegramBtn = document.getElementById('shareTelegramBtn');
        const shareFacebookBtn = document.getElementById('shareFacebookBtn');
        const shareTwitterBtn = document.getElementById('shareTwitterBtn');
        const copyRecipeLinkBtn = document.getElementById('copyRecipeLinkBtn');
        
        if (shareBtn) shareBtn.addEventListener('click', window.openShareModal);
        if (closeShareModalBtn) closeShareModalBtn.addEventListener('click', window.closeShareModal);
        if (shareWhatsAppBtn) shareWhatsAppBtn.addEventListener('click', window.shareToWhatsApp);
        if (shareTelegramBtn) shareTelegramBtn.addEventListener('click', window.shareToTelegram);
        if (shareFacebookBtn) shareFacebookBtn.addEventListener('click', window.shareToFacebook);
        if (shareTwitterBtn) shareTwitterBtn.addEventListener('click', window.shareToTwitter);
        if (copyRecipeLinkBtn) copyRecipeLinkBtn.addEventListener('click', window.copyRecipeLink);
    }

    // Collection Modal
    function initCollectionModal() {
        const recipeId = window.recipeData.recipeId;
        let userCollections = [];
        let recipeCollectionIds = [];
        
        async function loadCollections() {
            if (!isAuthenticated()) {
                return;
            }
            
            try {
                const response = await apiFetch(`${apiBaseUrl}/api/users/collections`);
                if (response.ok) {
                    userCollections = await response.json();
                    renderCollectionsList();
                }
            } catch (error) {
                console.error('Error loading collections:', error);
            }
        }
        
        async function loadRecipeCollections() {
            try {
                const response = await apiFetch(`${apiBaseUrl}/api/recipes/${recipeId}/collections`);
                if (response.ok) {
                    recipeCollectionIds = await response.json();
                }
            } catch (error) {
                console.error('Error loading recipe collections:', error);
            }
        }
        
        function renderCollectionsList() {
            const container = document.getElementById('collectionsList');
            if (!container) return;
            
            if (userCollections.length === 0) {
                container.innerHTML = '<p class="text-center p-20 c-grey-600">Henüz koleksiyonunuz yok. Yeni bir koleksiyon oluşturun!</p>';
                return;
            }
            
            container.innerHTML = userCollections.map(collection => {
                const isInCollection = recipeCollectionIds.includes(collection.id);
                return `
                    <div class="d-flex ai-c jc-sb p-10 bd bdrs-3 mB-10" style="cursor: pointer; transition: all 0.2s;" 
                         onmouseover="this.style.backgroundColor='#f5f5f5'" 
                         onmouseout="this.style.backgroundColor='transparent'"
                         onclick="toggleRecipeInCollection(${collection.id})">
                        <div>
                            <h6 class="mB-5">${escapeHtml(collection.name)}</h6>
                            ${collection.description ? `<p class="fsz-xs c-grey-600 mB-0">${escapeHtml(collection.description)}</p>` : ''}
                            <span class="fsz-xs c-grey-500">${collection.recipeCount} tarif</span>
                        </div>
                        <div>
                            <i class="ti-check c-green-500" style="font-size: 20px; ${isInCollection ? '' : 'display: none;'}" id="checkIcon_${collection.id}"></i>
                        </div>
                    </div>
                `;
            }).join('');
        }
        
        function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }
        
        window.toggleRecipeInCollection = async function(collectionId) {
            if (!isAuthenticated()) {
                requireLogin('Koleksiyona eklemek için giriş yapmanız gerekiyor.');
                return;
            }
            
            const isInCollection = recipeCollectionIds.includes(collectionId);
            const checkIcon = document.getElementById(`checkIcon_${collectionId}`);
            
            try {
                if (isInCollection) {
                    const response = await apiFetch(`${apiBaseUrl}/api/users/collections/${collectionId}/recipes/${recipeId}`, {
                        method: 'DELETE'
                    });
                    if (response.ok) {
                        recipeCollectionIds = recipeCollectionIds.filter(id => id !== collectionId);
                        if (checkIcon) checkIcon.style.display = 'none';
                    }
                } else {
                    const response = await apiFetch(`${apiBaseUrl}/api/users/collections/${collectionId}/recipes/${recipeId}`, {
                        method: 'POST'
                    });
                    if (response.ok) {
                        recipeCollectionIds.push(collectionId);
                        if (checkIcon) checkIcon.style.display = 'block';
                    }
                }
            } catch (error) {
                console.error('Error toggling recipe in collection:', error);
                showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
            }
        };
        
        window.openCollectionModal = function() {
            if (!isAuthenticated()) {
                requireLogin('Koleksiyona eklemek için giriş yapmanız gerekiyor.');
                return;
            }
            
            document.getElementById('collectionModal').style.display = 'block';
            document.getElementById('collectionModal').classList.add('show');
            loadCollections();
            loadRecipeCollections().then(() => {
                setTimeout(() => renderCollectionsList(), 100);
            });
        };
        
        window.closeCollectionModal = function() {
            document.getElementById('collectionModal').style.display = 'none';
            document.getElementById('collectionModal').classList.remove('show');
            hideCreateCollectionForm();
        };
        
        window.showCreateCollectionForm = function() {
            document.getElementById('createCollectionForm').style.display = 'block';
            document.getElementById('newCollectionName').focus();
        };
        
        window.hideCreateCollectionForm = function() {
            document.getElementById('createCollectionForm').style.display = 'none';
            document.getElementById('newCollectionName').value = '';
            document.getElementById('newCollectionDescription').value = '';
        };
        
        window.createCollection = async function() {
            if (!isAuthenticated()) {
                requireLogin('Koleksiyon oluşturmak için giriş yapmanız gerekiyor.');
                return;
            }
            
            const name = document.getElementById('newCollectionName').value.trim();
            const description = document.getElementById('newCollectionDescription').value.trim();
            
            if (!name) {
                showToast('Koleksiyon adı gereklidir.', 'warning');
                return;
            }
            
            try {
                const response = await apiFetch(`${apiBaseUrl}/api/users/collections`, {
                    method: 'POST',
                    body: JSON.stringify({
                        name: name,
                        description: description || null
                    })
                });
                
                if (response.ok) {
                    const newCollection = await response.json();
                    userCollections.push(newCollection);
                    renderCollectionsList();
                    hideCreateCollectionForm();
                    showToast('Koleksiyon oluşturuldu!', 'success');
                    await toggleRecipeInCollection(newCollection.id);
                } else {
                    const error = await response.json();
                    showToast(error.error || 'Koleksiyon oluşturulurken bir hata oluştu.', 'error');
                }
            } catch (error) {
                console.error('Error creating collection:', error);
                showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
            }
        };
        
        // Modal dışına tıklanınca kapat
        window.addEventListener('click', function(event) {
            const modal = document.getElementById('collectionModal');
            if (event.target === modal) {
                closeCollectionModal();
            }
        });
        
        // Event listeners
        const addToCollectionBtn = document.getElementById('addToCollectionBtn');
        const closeCollectionModalBtn = document.getElementById('closeCollectionModalBtn');
        const showCreateCollectionFormBtn = document.getElementById('showCreateCollectionFormBtn');
        const hideCreateCollectionFormBtn = document.getElementById('hideCreateCollectionFormBtn');
        const createCollectionBtn = document.getElementById('createCollectionBtn');
        
        if (addToCollectionBtn) addToCollectionBtn.addEventListener('click', window.openCollectionModal);
        if (closeCollectionModalBtn) closeCollectionModalBtn.addEventListener('click', window.closeCollectionModal);
        if (showCreateCollectionFormBtn) showCreateCollectionFormBtn.addEventListener('click', window.showCreateCollectionForm);
        if (hideCreateCollectionFormBtn) hideCreateCollectionFormBtn.addEventListener('click', window.hideCreateCollectionForm);
        if (createCollectionBtn) createCollectionBtn.addEventListener('click', window.createCollection);
    }

    // Comment System
    const recipeId = window.recipeData.recipeId;
    let currentCommentPage = 0;
    const commentsPerPage = 10;
    let allCommentsLoaded = false;
    let currentSort = 'newest';
    let isLoadingComments = false;
    let allComments = [];
    let editingCommentId = null;
    
    function initCommentSystem() {
        // Character counter
        const commentContent = document.getElementById('commentContent');
        const commentCharCount = document.getElementById('commentCharCount');
        const submitCommentBtn = document.getElementById('submitCommentBtn');
        
        if (commentContent && commentCharCount && submitCommentBtn) {
            commentContent.addEventListener('input', function() {
                const length = this.value.trim().length;
                commentCharCount.textContent = length;
                if (length > 1000) {
                    commentCharCount.style.color = '#dc3545';
                    submitCommentBtn.disabled = true;
                    submitCommentBtn.classList.add('disabled');
                } else if (length === 0) {
                    commentCharCount.style.color = '#6c757d';
                    submitCommentBtn.disabled = true;
                    submitCommentBtn.classList.add('disabled');
                } else {
                    commentCharCount.style.color = '#6c757d';
                    submitCommentBtn.disabled = false;
                    submitCommentBtn.classList.remove('disabled');
                }
            });
            submitCommentBtn.disabled = true;
            submitCommentBtn.classList.add('disabled');
        }
        
        // Event listeners for comment buttons
        const submitBtn = document.getElementById('submitCommentBtn');
        if (submitBtn) {
            submitBtn.addEventListener('click', window.submitComment);
        }
        
        loadComments(true);
        setupInfiniteScroll();
    }
    
    async function loadComments(reset = false) {
        if (isLoadingComments) return;
        
        if (reset) {
            currentCommentPage = 0;
            allCommentsLoaded = false;
            allComments = [];
        }
        
        if (allCommentsLoaded && !reset) return;
        
        isLoadingComments = true;
        const commentsList = document.getElementById('commentsList');
        const loadMoreBtn = document.getElementById('loadMoreComments');
        const commentCount = document.getElementById('commentCount');
        
        if (reset && commentsList) {
            commentsList.innerHTML = `
                <div class="ta-c p-20">
                    <i class="ti-reload c-grey-400" style="font-size: 24px; animation: spin 1s linear infinite;"></i>
                    <p class="mT-10 c-grey-600">Yorumlar yükleniyor...</p>
                </div>
            `;
        }
        
        try {
            const skip = currentCommentPage * commentsPerPage;
            const url = `${apiBaseUrl}/api/recipes/${recipeId}/comments?skip=${skip}&take=${commentsPerPage}`;
            
            const response = await apiFetch(url);
            if (!response.ok) {
                throw new Error('Yorumlar yüklenemedi');
            }
            
            const data = await response.json();
            const comments = data.comments || [];
            const totalCount = data.totalCount || 0;
            
            if (commentCount) {
                commentCount.textContent = totalCount;
            }
            
            if (reset) {
                allComments = [];
                commentsList.innerHTML = '';
            }
            
            if (comments.length === 0 && reset) {
                commentsList.innerHTML = `
                    <div class="ta-c p-20">
                        <i class="ti-comment-alt c-grey-400" style="font-size: 24px;"></i>
                        <p class="mT-10 c-grey-600">Henüz yorum yapılmamış. İlk yorumu siz yapın!</p>
                    </div>
                `;
                isLoadingComments = false;
                return;
            }
            
            allComments = allComments.concat(comments);
            renderComments();
            
            if (comments.length < commentsPerPage || skip + comments.length >= totalCount) {
                allCommentsLoaded = true;
                if (loadMoreBtn) loadMoreBtn.style.display = 'none';
            } else {
                if (loadMoreBtn) loadMoreBtn.style.display = 'block';
            }
            
            currentCommentPage++;
        } catch (error) {
            console.error('Error loading comments:', error);
            commentsList.innerHTML = `
                <div class="ta-c p-20">
                    <i class="ti-alert c-red-500" style="font-size: 24px;"></i>
                    <p class="mT-10 c-red-500">Yorumlar yüklenirken bir hata oluştu.</p>
                </div>
            `;
            showToast('Yorumlar yüklenirken bir hata oluştu.', 'error');
        } finally {
            isLoadingComments = false;
        }
    }
    
    function renderComments() {
        const commentsList = document.getElementById('commentsList');
        if (!commentsList) return;
        
        let sortedComments = [...allComments];
        
        if (currentSort === 'liked') {
            sortedComments.sort((a, b) => b.likeCount - a.likeCount);
        } else {
            sortedComments.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
        }
        
        commentsList.innerHTML = '';
        sortedComments.forEach(comment => {
            commentsList.appendChild(createCommentElement(comment));
        });
    }
    
    // Helper function for HTML escaping (used in multiple places)
    window.escapeHtml = function(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    };
    
    function escapeHtml(text) {
        return window.escapeHtml(text);
    }
    
    function getTimeAgo(date) {
        const now = new Date();
        const diff = now - date;
        const seconds = Math.floor(diff / 1000);
        const minutes = Math.floor(seconds / 60);
        const hours = Math.floor(minutes / 60);
        const days = Math.floor(hours / 24);
        
        if (days > 0) return `${days} gün önce`;
        if (hours > 0) return `${hours} saat önce`;
        if (minutes > 0) return `${minutes} dakika önce`;
        return 'Az önce';
    }
    
    function createCommentElement(comment, isReply = false) {
        const div = document.createElement('div');
        div.className = `bd bgc-white p-20 bdrs-3 mB-15 comment-item ${isReply ? 'comment-reply' : ''}`;
        div.setAttribute('data-comment-id', comment.id);
        if (isReply) {
            div.style.marginLeft = '40px';
            div.style.borderLeft = '3px solid #e0e0e0';
        }
        
        const createdAt = comment.createdAt;
        const updatedAt = comment.updatedAt;
        const userName = comment.userName || 'Anonim';
        const content = comment.content || '';
        const likeCount = comment.likeCount || 0;
        const isLikedByUser = comment.isLikedByUser || false;
        const canEdit = comment.canEdit || false;
        const canDelete = comment.canDelete || false;
        const replies = comment.replies || [];
        const replyCount = comment.replyCount || replies.length;
        
        const timeAgo = getTimeAgo(new Date(createdAt));
        const isEdited = updatedAt && updatedAt !== createdAt;
        
        div.innerHTML = `
            <div class="d-flex justify-content-between align-items-start mB-10">
                <div class="d-flex align-items-center">
                    <div class="w-40 h-40 bdrs-50p bgc-blue-500 d-flex ai-c jc-c mR-10" style="min-width: 40px; min-height: 40px;">
                        <span class="c-white fw-600">${userName.charAt(0).toUpperCase()}</span>
                    </div>
                    <div>
                        <h6 class="m-0 fw-600">${escapeHtml(userName)}</h6>
                        <small class="c-grey-600">${timeAgo}${isEdited ? ' (düzenlendi)' : ''}</small>
                    </div>
                </div>
                ${canEdit || canDelete ? `
                    <div class="dropdown">
                        <button class="btn btn-link p-0" type="button" data-bs-toggle="dropdown">
                            <i class="ti-more-vertical c-grey-600"></i>
                        </button>
                        <ul class="dropdown-menu">
                            ${canEdit ? `<li><a class="dropdown-item" href="#" onclick="editComment(${comment.id}); return false;"><i class="ti-pencil mR-5"></i> Düzenle</a></li>` : ''}
                            ${canDelete ? `<li><a class="dropdown-item text-danger" href="#" onclick="deleteComment(${comment.id}); return false;"><i class="ti-trash mR-5"></i> Sil</a></li>` : ''}
                        </ul>
                    </div>
                ` : ''}
            </div>
            <div class="comment-content mB-10">
                <p class="m-0" id="comment-text-${comment.id}">${escapeHtml(content)}</p>
            </div>
            <div class="d-flex align-items-center gap-10" style="gap: 12px;">
                <button class="like-comment-btn ${isLikedByUser ? 'liked' : ''}" 
                        onclick="toggleCommentLike(${comment.id})" 
                        data-comment-id="${comment.id}">
                    <i class="ti-heart"></i>
                    <span class="like-count-${comment.id}">${likeCount}</span>
                </button>
                ${!isReply && isAuthenticated() ? `
                    <button class="reply-comment-btn" 
                            onclick="showReplyForm(${comment.id})" 
                            data-comment-id="${comment.id}"
                            title="Yanıtla">
                        <i class="ti-comment"></i>
                        ${replyCount > 0 ? `<span>${replyCount}</span>` : ''}
                    </button>
                ` : ''}
            </div>
        ${!isReply && replies && replies.length > 0 ? `
            <div class="comment-replies mT-15" id="replies-${comment.id}">
                ${replies.map(reply => createCommentElement(reply, true).outerHTML).join('')}
            </div>
            ` : ''}
            ${!isReply && isAuthenticated() ? `
                <div class="reply-form-container mT-15" id="reply-form-${comment.id}" style="display: none;">
                    <div class="bd bgc-grey-50 p-15 bdrs-3">
                        <textarea class="form-control mB-10" id="reply-content-${comment.id}" rows="2" placeholder="Yanıtınızı yazın..." maxlength="1000"></textarea>
                        <div class="d-flex justify-content-between align-items-center">
                            <small class="c-grey-600">
                                <span id="reply-char-count-${comment.id}">0</span>/1000
                            </small>
                            <div>
                                <button class="btn btn-sm btn-secondary" onclick="hideReplyForm(${comment.id})">İptal</button>
                                <button class="btn btn-sm btn-primary mL-5" onclick="submitReply(${comment.id})">Yanıtla</button>
                            </div>
                        </div>
                    </div>
                </div>
            ` : ''}
        `;
        
        return div;
    }
    
    window.sortComments = function(sortType) {
        currentSort = sortType;
        document.getElementById('sortNewest')?.classList.toggle('active', sortType === 'newest');
        document.getElementById('sortLiked')?.classList.toggle('active', sortType === 'liked');
        renderComments();
    };
    
    window.submitComment = async function() {
        if (!isAuthenticated()) {
            requireLogin('Yorum yapmak için giriş yapmanız gerekiyor.');
            return false;
        }
        
        const commentContent = document.getElementById('commentContent');
        const content = commentContent?.value.trim();
        if (!content) {
            showToast('Lütfen yorum yazın.', 'warning');
            return;
        }
        if (content.length > 1000) {
            showToast('Yorum en fazla 1000 karakter olabilir.', 'warning');
            return;
        }
        
        const tempComment = {
            id: 'temp-' + Date.now(),
            userId: '',
            userName: 'Yükleniyor...',
            content: content,
            likeCount: 0,
            isLikedByUser: false,
            createdAt: new Date().toISOString(),
            canEdit: true,
            canDelete: true
        };
        const commentsList = document.getElementById('commentsList');
        if (commentsList && commentsList.querySelector('.ta-c')) {
            commentsList.innerHTML = '';
        }
        commentsList?.insertBefore(createCommentElement(tempComment), commentsList.firstChild);
        
        try {
            const response = await apiFetch(`${apiBaseUrl}/api/recipes/${recipeId}/comments`, {
                method: 'POST',
                body: JSON.stringify({
                    content: content,
                    parentCommentId: null
                })
            });
            
            if (response.ok) {
                commentContent.value = '';
                const commentCharCount = document.getElementById('commentCharCount');
                if (commentCharCount) {
                    commentCharCount.textContent = '0';
                    commentCharCount.style.color = '#6c757d';
                }
                const submitCommentBtn = document.getElementById('submitCommentBtn');
                if (submitCommentBtn) {
                    submitCommentBtn.disabled = true;
                    submitCommentBtn.classList.add('disabled');
                }
                showToast('Yorumunuz başarıyla eklendi!', 'success');
                await loadComments(true);
            } else {
                document.querySelector(`[data-comment-id="${tempComment.id}"]`)?.remove();
                const error = await response.json();
                showToast(error.errors?.Content?.[0] || error.error || 'Yorum eklenirken bir hata oluştu.', 'error');
            }
        } catch (error) {
            document.querySelector(`[data-comment-id="${tempComment.id}"]`)?.remove();
            console.error('Error submitting comment:', error);
            showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    };
    
    window.editComment = async function(commentId) {
        const commentItem = document.querySelector(`[data-comment-id="${commentId}"]`);
        const commentText = commentItem.querySelector(`#comment-text-${commentId}`);
        const currentText = commentText.textContent;
        
        if (editingCommentId === commentId) {
            editingCommentId = null;
            commentText.textContent = currentText;
            return;
        }
        
        editingCommentId = commentId;
        const textarea = document.createElement('textarea');
        textarea.className = 'form-control';
        textarea.value = currentText;
        textarea.rows = 3;
        textarea.maxLength = 1000;
        
        const saveBtn = document.createElement('button');
        saveBtn.className = 'btn btn-primary btn-sm mT-10 mR-5';
        saveBtn.innerHTML = '<i class="ti-check mR-5"></i> Kaydet';
        saveBtn.onclick = async () => {
            await window.updateComment(commentId, textarea.value.trim());
        };
        
        const cancelBtn = document.createElement('button');
        cancelBtn.className = 'btn btn-secondary btn-sm mT-10';
        cancelBtn.innerHTML = '<i class="ti-close mR-5"></i> İptal';
        cancelBtn.onclick = () => {
            editingCommentId = null;
            commentText.textContent = currentText;
            textarea.replaceWith(commentText);
            saveBtn.remove();
            cancelBtn.remove();
        };
        
        commentText.replaceWith(textarea);
        commentItem.querySelector('.comment-content').appendChild(saveBtn);
        commentItem.querySelector('.comment-content').appendChild(cancelBtn);
    };
    
    window.updateComment = async function(commentId, newContent) {
        if (!newContent) {
            showToast('Yorum içeriği boş olamaz.', 'warning');
            return;
        }
        if (newContent.length > 1000) {
            showToast('Yorum en fazla 1000 karakter olabilir.', 'warning');
            return;
        }
        
        const commentItem = document.querySelector(`[data-comment-id="${commentId}"]`);
        const commentText = commentItem?.querySelector(`#comment-text-${commentId}`);
        const oldText = commentText?.textContent;
        
        try {
            const response = await apiFetch(`${apiBaseUrl}/api/comments/${commentId}`, {
                method: 'PUT',
                body: JSON.stringify({
                    content: newContent
                })
            });
            
            if (response.ok) {
                editingCommentId = null;
                showToast('Yorumunuz güncellendi!', 'success');
                await loadComments(true);
            } else {
                if (commentText && oldText) {
                    commentText.textContent = oldText;
                }
                const error = await response.json();
                showToast(error.errors?.Content?.[0] || error.error || 'Yorum güncellenirken bir hata oluştu.', 'error');
            }
        } catch (error) {
            if (commentText && oldText) {
                commentText.textContent = oldText;
            }
            console.error('Error updating comment:', error);
            showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    };
    
    window.deleteComment = async function(commentId) {
        let confirmed = false;
        if (typeof Swal !== 'undefined') {
            const result = await Swal.fire({
                icon: 'warning',
                title: 'Yorumu Silmek İstediğinize Emin misiniz?',
                text: 'Bu işlem geri alınamaz.',
                showCancelButton: true,
                confirmButtonText: 'Evet, Sil',
                cancelButtonText: 'İptal',
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                reverseButtons: true
            });
            confirmed = result.isConfirmed;
        } else {
            confirmed = confirm('Bu yorumu silmek istediğinize emin misiniz?');
        }
        
        if (!confirmed) {
            return;
        }
        
        if (!isAuthenticated()) {
            requireLogin('Yorum silmek için giriş yapmanız gerekiyor.');
            return;
        }
        
        const commentItem = document.querySelector(`[data-comment-id="${commentId}"]`);
        if (commentItem) {
            commentItem.style.opacity = '0.5';
            commentItem.style.pointerEvents = 'none';
        }
        
        try {
            const response = await apiFetch(`${apiBaseUrl}/api/comments/${commentId}`, {
                method: 'DELETE'
            });
            
            if (response.ok) {
                if (commentItem) {
                    commentItem.style.transition = 'opacity 0.3s';
                    commentItem.style.opacity = '0';
                    setTimeout(() => commentItem.remove(), 300);
                }
                showToast('Yorumunuz silindi.', 'success');
                const commentCount = document.getElementById('commentCount');
                if (commentCount) {
                    const currentCount = parseInt(commentCount.textContent) || 0;
                    commentCount.textContent = Math.max(0, currentCount - 1);
                }
            } else {
                if (commentItem) {
                    commentItem.style.opacity = '1';
                    commentItem.style.pointerEvents = 'auto';
                }
                const error = await response.json();
                showToast(error.error || 'Yorum silinirken bir hata oluştu.', 'error');
            }
        } catch (error) {
            if (commentItem) {
                commentItem.style.opacity = '1';
                commentItem.style.pointerEvents = 'auto';
            }
            console.error('Error deleting comment:', error);
            showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    };
    
    window.toggleCommentLike = async function(commentId) {
        if (!isAuthenticated()) {
            requireLogin('Beğenmek için giriş yapmanız gerekiyor.');
            return;
        }
        
        const likeBtn = document.querySelector(`.like-comment-btn[data-comment-id="${commentId}"]`);
        const likeIcon = likeBtn?.querySelector('i');
        const likeCountSpan = document.querySelector(`.like-count-${commentId}`);
        
        const currentCount = parseInt(likeCountSpan?.textContent || '0');
        const isCurrentlyLiked = likeIcon?.classList.contains('c-red-500');
        const newCount = isCurrentlyLiked ? currentCount - 1 : currentCount + 1;
        const newIsLiked = !isCurrentlyLiked;
        
        if (likeCountSpan) {
            likeCountSpan.textContent = Math.max(0, newCount);
        }
        if (likeIcon) {
            if (newIsLiked) {
                likeIcon.classList.remove('c-grey-400');
                likeIcon.classList.add('c-red-500');
                likeBtn.classList.add('liked');
            } else {
                likeIcon.classList.remove('c-red-500');
                likeIcon.classList.add('c-grey-400');
                likeBtn.classList.remove('liked');
            }
        }
        
        try {
            const response = await apiFetch(`${apiBaseUrl}/api/comments/${commentId}/like`, {
                method: 'POST'
            });
            
            if (response.ok) {
                const data = await response.json();
                if (likeCountSpan) {
                    likeCountSpan.textContent = data.likeCount || 0;
                }
                if (likeIcon) {
                    if (data.isLiked) {
                        likeIcon.classList.remove('c-grey-400');
                        likeIcon.classList.add('c-red-500');
                        likeBtn.classList.add('liked');
                    } else {
                        likeIcon.classList.remove('c-red-500');
                        likeIcon.classList.add('c-grey-400');
                        likeBtn.classList.remove('liked');
                    }
                }
            } else {
                if (likeCountSpan) {
                    likeCountSpan.textContent = currentCount;
                }
                if (likeIcon) {
                    if (isCurrentlyLiked) {
                        likeIcon.classList.remove('c-grey-400');
                        likeIcon.classList.add('c-red-500');
                        likeBtn.classList.add('liked');
                    } else {
                        likeIcon.classList.remove('c-red-500');
                        likeIcon.classList.add('c-grey-400');
                        likeBtn.classList.remove('liked');
                    }
                }
                const error = await response.json();
                showToast(error.error || 'Beğeni işlemi sırasında bir hata oluştu.', 'error');
            }
        } catch (error) {
            if (likeCountSpan) {
                likeCountSpan.textContent = currentCount;
            }
            if (likeIcon) {
                if (isCurrentlyLiked) {
                    likeIcon.classList.remove('c-grey-400');
                    likeIcon.classList.add('c-red-500');
                    likeBtn.classList.add('liked');
                } else {
                    likeIcon.classList.remove('c-red-500');
                    likeIcon.classList.add('c-grey-400');
                    likeBtn.classList.remove('liked');
                }
            }
            console.error('Error toggling comment like:', error);
            showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    };
    
    window.showReplyForm = function(commentId) {
        if (!isAuthenticated()) {
            requireLogin('Yanıt vermek için giriş yapmanız gerekiyor.');
            return;
        }
        
        const replyForm = document.getElementById(`reply-form-${commentId}`);
        const replyTextarea = document.getElementById(`reply-content-${commentId}`);
        const replyCharCount = document.getElementById(`reply-char-count-${commentId}`);
        
        if (replyForm) {
            replyForm.style.display = 'block';
            if (replyTextarea) {
                replyTextarea.focus();
                replyTextarea.addEventListener('input', function() {
                    const length = this.value.trim().length;
                    if (replyCharCount) {
                        replyCharCount.textContent = length;
                        replyCharCount.style.color = length > 1000 ? '#dc3545' : '#6c757d';
                    }
                });
            }
        }
    };
    
    window.hideReplyForm = function(commentId) {
        const replyForm = document.getElementById(`reply-form-${commentId}`);
        const replyTextarea = document.getElementById(`reply-content-${commentId}`);
        if (replyForm) {
            replyForm.style.display = 'none';
        }
        if (replyTextarea) {
            replyTextarea.value = '';
            const replyCharCount = document.getElementById(`reply-char-count-${commentId}`);
            if (replyCharCount) {
                replyCharCount.textContent = '0';
                replyCharCount.style.color = '#6c757d';
            }
        }
    };
    
    window.submitReply = async function(parentCommentId) {
        const replyTextarea = document.getElementById(`reply-content-${parentCommentId}`);
        const content = replyTextarea?.value.trim();
        
        if (!content) {
            showToast('Lütfen yanıt yazın.', 'warning');
            return;
        }
        if (content.length > 1000) {
            showToast('Yanıt en fazla 1000 karakter olabilir.', 'warning');
            return;
        }
        
        if (!isAuthenticated()) {
            requireLogin('Yanıt vermek için giriş yapmanız gerekiyor.');
            return;
        }
        
        try {
            const response = await apiFetch(`${apiBaseUrl}/api/recipes/${recipeId}/comments`, {
                method: 'POST',
                body: JSON.stringify({
                    content: content,
                    parentCommentId: parentCommentId
                })
            });
            
            if (response.ok) {
                hideReplyForm(parentCommentId);
                showToast('Yanıtınız başarıyla eklendi!', 'success');
                await loadComments(true);
            } else {
                const error = await response.json();
                showToast(error.errors?.Content?.[0] || error.error || 'Yanıt eklenirken bir hata oluştu.', 'error');
            }
        } catch (error) {
            console.error('Error submitting reply:', error);
            showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
        }
    };
    
    window.loadMoreComments = function() {
        loadComments(false);
    };
    
    let intersectionObserver = null;
    function setupInfiniteScroll() {
        const trigger = document.getElementById('infiniteScrollTrigger');
        if (!trigger) return;
        
        if ('IntersectionObserver' in window) {
            intersectionObserver = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting && !isLoadingComments && !allCommentsLoaded) {
                        loadComments(false);
                    }
                });
            }, {
                root: null,
                rootMargin: '100px',
                threshold: 0.1
            });
            
            intersectionObserver.observe(trigger);
        } else {
            window.addEventListener('scroll', () => {
                const triggerRect = trigger.getBoundingClientRect();
                if (triggerRect.top < window.innerHeight && !isLoadingComments && !allCommentsLoaded) {
                    loadComments(false);
                }
            });
        }
    }

    // Image Lightbox
    function initImageLightbox() {
        // Event delegation for lightbox images
        document.addEventListener('click', function(e) {
            const img = e.target.closest('img[data-lightbox-image]');
            if (img) {
                const imageUrl = img.getAttribute('data-lightbox-image');
                const imageTitle = img.getAttribute('data-lightbox-title') || '';
                openImageLightbox(imageUrl, imageTitle);
            }
        });
    }
    
    window.openImageLightbox = function(imageUrl, imageTitle) {
        // Simple lightbox implementation
        const lightbox = document.createElement('div');
        lightbox.id = 'imageLightbox';
        lightbox.style.cssText = 'position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.9); z-index: 10000; display: flex; align-items: center; justify-content: center; cursor: pointer;';
        
        const img = document.createElement('img');
        img.src = imageUrl;
        img.alt = imageTitle || '';
        img.style.cssText = 'max-width: 90%; max-height: 90%; object-fit: contain;';
        
        lightbox.appendChild(img);
        document.body.appendChild(lightbox);
        
        lightbox.addEventListener('click', function() {
            document.body.removeChild(lightbox);
        });
        
        document.addEventListener('keydown', function escHandler(e) {
            if (e.key === 'Escape') {
                if (document.getElementById('imageLightbox')) {
                    document.body.removeChild(lightbox);
                }
                document.removeEventListener('keydown', escHandler);
            }
        });
    };
    
    // Load similar recipes
    async function loadSimilarRecipes() {
        try {
            const categoryId = window.recipeData.categoryId || 0;
            const response = await fetch(`${apiBaseUrl}/api/recipes/category/${categoryId}?pageNumber=1&pageSize=4`);
            if (response.ok) {
                const data = await response.json();
                const similarRecipes = data.items?.filter(r => r.id !== window.recipeData.recipeId).slice(0, 4) || [];
                renderSimilarRecipes(similarRecipes);
            }
        } catch (error) {
            console.error('Error loading similar recipes:', error);
            const container = document.getElementById('similarRecipes');
            if (container) {
                container.innerHTML = '<div class="col-12 text-center p-20"><p class="c-grey-600">Benzer tarifler yüklenemedi.</p></div>';
            }
        }
    }
    
    // Load author recipes
    async function loadAuthorRecipes(authorId) {
        try {
            const response = await fetch(`${apiBaseUrl}/api/authors/${authorId}/recipes?pageNumber=1&pageSize=4`);
            if (response.ok) {
                const data = await response.json();
                const authorRecipes = data.items?.filter(r => r.id !== window.recipeData.recipeId).slice(0, 4) || [];
                renderAuthorRecipes(authorRecipes);
            }
        } catch (error) {
            console.error('Error loading author recipes:', error);
            const container = document.getElementById('authorRecipes');
            if (container) {
                container.innerHTML = '<div class="col-12 text-center p-20"><p class="c-grey-600">Yazarın diğer tarifleri yüklenemedi.</p></div>';
            }
        }
    }
    
    // Render similar recipes
    function renderSimilarRecipes(recipes) {
        const container = document.getElementById('similarRecipes');
        if (!container) return;
        
        if (!recipes || recipes.length === 0) {
            container.innerHTML = '<div class="col-12 text-center p-20"><p class="c-grey-600">Benzer tarif bulunamadı.</p></div>';
            return;
        }
        
        container.innerHTML = recipes.map(recipe => `
            <div class="col-md-6 col-lg-3">
                <a href="/Recipes/Details/${recipe.id}" class="recipe-card-link" style="text-decoration: none; display: block;">
                    <div class="bd bgc-white bdrs-3 ov-h recipe-card" style="transition: all 0.3s ease; cursor: pointer; height: 100%;">
                        <div class="recipe-card-image-wrapper" style="position: relative; overflow: hidden; height: 150px;">
                            <img src="${recipe.imageUrl || '/images/placeholder.jpg'}" alt="${recipe.title}" 
                                 style="width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s ease;">
                        </div>
                        <div class="p-15">
                            <h6 class="mB-5" style="font-size: 14px; line-height: 1.4; min-height: 40px;">${escapeHtml(recipe.title)}</h6>
                            <div class="peers fxw-nw ai-c fsz-sm c-grey-600">
                                <div class="peer mR-10"><i class="ti-time"></i> ${recipe.cookingTimeMinutes} dk</div>
                                ${recipe.averageRating ? `<div class="peer"><i class="ti-star c-yellow-500"></i> ${recipe.averageRating.toFixed(1)}</div>` : ''}
                            </div>
                        </div>
                    </div>
                </a>
            </div>
        `).join('');
    }
    
    // Render author recipes
    function renderAuthorRecipes(recipes) {
        const container = document.getElementById('authorRecipes');
        if (!container) return;
        
        if (!recipes || recipes.length === 0) {
            container.innerHTML = '<div class="col-12 text-center p-20"><p class="c-grey-600">Yazarın başka tarifi bulunamadı.</p></div>';
            return;
        }
        
        container.innerHTML = recipes.map(recipe => `
            <div class="col-md-6 col-lg-3">
                <a href="/Recipes/Details/${recipe.id}" class="recipe-card-link" style="text-decoration: none; display: block;">
                    <div class="bd bgc-white bdrs-3 ov-h recipe-card" style="transition: all 0.3s ease; cursor: pointer; height: 100%;">
                        <div class="recipe-card-image-wrapper" style="position: relative; overflow: hidden; height: 150px;">
                            <img src="${recipe.imageUrl || '/images/placeholder.jpg'}" alt="${recipe.title}" 
                                 style="width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s ease;">
                        </div>
                        <div class="p-15">
                            <h6 class="mB-5" style="font-size: 14px; line-height: 1.4; min-height: 40px;">${escapeHtml(recipe.title)}</h6>
                            <div class="peers fxw-nw ai-c fsz-sm c-grey-600">
                                <div class="peer mR-10"><i class="ti-time"></i> ${recipe.cookingTimeMinutes} dk</div>
                                ${recipe.averageRating ? `<div class="peer"><i class="ti-star c-yellow-500"></i> ${recipe.averageRating.toFixed(1)}</div>` : ''}
                            </div>
                        </div>
                    </div>
                </a>
            </div>
        `).join('');
    }

})();

