// 文件管理系统JavaScript

// 等待DOM加载完成
document.addEventListener('DOMContentLoaded', function () {
    // 初始化工具提示
    initTooltips();
    
    // 初始化复制按钮
    initCopyButtons();
    
    // 初始化文件上传预览
    initFileUploadPreview();
    
    // 初始化密码保护切换
    initPasswordProtection();
    
    // 初始化确认对话框
    initConfirmDialogs();
    
    // 初始化文件拖放上传
    initDragAndDrop();
    
    // 初始化加载动画
    initLoadingIndicators();
    
    // 初始化表格排序
    initTableSorting();
});

// 初始化工具提示
function initTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// 初始化复制按钮
function initCopyButtons() {
    const copyButtons = document.querySelectorAll('.copy-btn');
    if (copyButtons) {
        copyButtons.forEach(button => {
            button.addEventListener('click', function() {
                const textToCopy = this.getAttribute('data-copy-text');
                copyToClipboard(textToCopy);
                
                // 临时更改按钮文本
                const originalText = this.innerHTML;
                this.innerHTML = '<i class="bi bi-check"></i> 已复制!';
                
                setTimeout(() => {
                    this.innerHTML = originalText;
                }, 2000);
            });
        });
    }
}

// 复制文本到剪贴板
function copyToClipboard(text) {
    const textarea = document.createElement('textarea');
    textarea.value = text;
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    document.body.removeChild(textarea);
    
    // 显示提示消息
    showToast('已复制到剪贴板', 'success');
}

// 初始化文件上传预览
function initFileUploadPreview() {
    const fileInput = document.getElementById('file');
    if (fileInput) {
        fileInput.addEventListener('change', function() {
            const filePreview = document.getElementById('file-preview');
            if (filePreview) {
                if (this.files && this.files[0]) {
                    const file = this.files[0];
                    const fileSize = formatFileSize(file.size);
                    
                    let previewHtml = `
                        <div class="alert alert-info">
                            <strong>已选择文件:</strong> ${file.name}<br>
                            <strong>大小:</strong> ${fileSize}<br>
                            <strong>类型:</strong> ${file.type}
                        </div>
                    `;
                    
                    // 如果是图片，显示预览
                    if (file.type.startsWith('image/')) {
                        const reader = new FileReader();
                        reader.onload = function(e) {
                            const imgPreview = document.createElement('img');
                            imgPreview.src = e.target.result;
                            imgPreview.className = 'img-thumbnail mt-2';
                            imgPreview.style.maxHeight = '200px';
                            filePreview.appendChild(imgPreview);
                        }
                        reader.readAsDataURL(file);
                    }
                    
                    filePreview.innerHTML = previewHtml;
                    filePreview.style.display = 'block';
                }
            }
        });
    }
}

// 格式化文件大小
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// 初始化密码保护切换
function initPasswordProtection() {
    const passwordCheckbox = document.getElementById('isPasswordProtected');
    if (passwordCheckbox) {
        passwordCheckbox.addEventListener('change', function() {
            const passwordSection = document.querySelector('.password-section');
            const passwordInput = document.getElementById('password');
            
            if (this.checked) {
                passwordSection.style.display = 'block';
                passwordInput.setAttribute('required', 'required');
            } else {
                passwordSection.style.display = 'none';
                passwordInput.removeAttribute('required');
            }
        });
    }
}

// 初始化确认对话框
function initConfirmDialogs() {
    const confirmForms = document.querySelectorAll('form[data-confirm]');
    if (confirmForms) {
        confirmForms.forEach(form => {
            form.addEventListener('submit', function(e) {
                const confirmMessage = this.getAttribute('data-confirm');
                if (!confirm(confirmMessage)) {
                    e.preventDefault();
                }
            });
        });
    }
}

// 初始化文件拖放上传
function initDragAndDrop() {
    const dropZone = document.getElementById('drop-zone');
    if (dropZone) {
        // 阻止默认拖放行为
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, preventDefaults, false);
        });
        
        function preventDefaults(e) {
            e.preventDefault();
            e.stopPropagation();
        }
        
        // 高亮显示拖放区域
        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, highlight, false);
        });
        
        ['dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, unhighlight, false);
        });
        
        function highlight() {
            dropZone.classList.add('drag-highlight');
        }
        
        function unhighlight() {
            dropZone.classList.remove('drag-highlight');
        }
        
        // 处理拖放的文件
        dropZone.addEventListener('drop', handleDrop, false);
        
        function handleDrop(e) {
            const dt = e.dataTransfer;
            const files = dt.files;
            
            if (files.length > 0) {
                const fileInput = document.getElementById('file');
                fileInput.files = files;
                
                // 触发change事件以更新预览
                const event = new Event('change');
                fileInput.dispatchEvent(event);
            }
        }
    }
}

// 初始化加载指示器
function initLoadingIndicators() {
    const forms = document.querySelectorAll('form:not([data-no-loading])');
    if (forms) {
        forms.forEach(form => {
            form.addEventListener('submit', function() {
                const submitButton = this.querySelector('button[type="submit"]');
                if (submitButton) {
                    const originalText = submitButton.innerHTML;
                    submitButton.disabled = true;
                    submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> 处理中...';
                    
                    // 如果表单提交时间过长，恢复按钮状态
                    setTimeout(() => {
                        submitButton.disabled = false;
                        submitButton.innerHTML = originalText;
                    }, 10000);
                }
            });
        });
    }
}

// 初始化表格排序
function initTableSorting() {
    const tables = document.querySelectorAll('.sortable-table');
    if (tables) {
        tables.forEach(table => {
            const headers = table.querySelectorAll('th[data-sort]');
            headers.forEach(header => {
                header.addEventListener('click', function() {
                    const sortKey = this.getAttribute('data-sort');
                    const sortDirection = this.getAttribute('data-direction') === 'asc' ? 'desc' : 'asc';
                    
                    // 更新所有表头的排序方向
                    headers.forEach(h => h.setAttribute('data-direction', ''));
                    this.setAttribute('data-direction', sortDirection);
                    
                    // 更新排序图标
                    headers.forEach(h => h.querySelector('.sort-icon')?.remove());
                    co
