// 通用进度条模态框组件
class ProgressModal {
    constructor() {
        this.modal = null;
        this.progressBar = null;
        this.statusText = null;
        this.cancelBtn = null;
        this.isVisible = false;
        this.currentOperation = null;
        this.init();
    }

    init() {
        // 创建模态框HTML
        const modalHtml = `
            <div class="modal fade" id="progressModal" tabindex="-1" aria-labelledby="progressModalLabel" aria-hidden="true" data-bs-backdrop="static" data-bs-keyboard="false">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="progressModalLabel">
                                <i class="bi bi-hourglass-split me-2"></i>处理中...
                            </h5>
                        </div>
                        <div class="modal-body">
                            <div class="mb-3">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span id="progressStatusText">准备开始...</span>
                                    <span id="progressPercentage">0%</span>
                                </div>
                                <div class="progress" style="height: 20px;">
                                    <div class="progress-bar progress-bar-striped progress-bar-animated" 
                                         role="progressbar" 
                                         style="width: 0%" 
                                         id="progressBarFill"
                                         aria-valuenow="0" 
                                         aria-valuemin="0" 
                                         aria-valuemax="100">
                                    </div>
                                </div>
                            </div>
                            <div id="progressDetails" class="small text-muted">
                                <!-- 详细信息将在这里显示 -->
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" id="progressCancelBtn" style="display: none;">
                                <i class="bi bi-x-circle me-2"></i>取消
                            </button>
                            <button type="button" class="btn btn-primary" id="progressOkBtn" style="display: none;">
                                <i class="bi bi-check-circle me-2"></i>确定
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // 添加到页面
        if (!document.getElementById('progressModal')) {
            document.body.insertAdjacentHTML('beforeend', modalHtml);
        }

        // 获取元素引用
        this.modal = document.getElementById('progressModal');
        this.progressBar = document.getElementById('progressBarFill');
        this.statusText = document.getElementById('progressStatusText');
        this.percentageText = document.getElementById('progressPercentage');
        this.detailsContainer = document.getElementById('progressDetails');
        this.cancelBtn = document.getElementById('progressCancelBtn');
        this.okBtn = document.getElementById('progressOkBtn');
        this.titleElement = document.getElementById('progressModalLabel');

        // 初始化Bootstrap模态框
        this.bsModal = new bootstrap.Modal(this.modal);

        // 绑定取消按钮事件
        this.cancelBtn.addEventListener('click', () => {
            this.cancel();
        });

        // 绑定确定按钮事件
        this.okBtn.addEventListener('click', () => {
            this.hide();
        });
    }

    show(options = {}) {
        const defaultOptions = {
            title: '处理中...',
            titleIcon: 'bi-hourglass-split',
            status: '准备开始...',
            showCancel: false,
            showProgress: true
        };

        const config = { ...defaultOptions, ...options };

        // 设置标题和图标
        this.titleElement.innerHTML = `<i class="bi ${config.titleIcon} me-2"></i>${config.title}`;
        
        // 设置初始状态
        this.updateProgress(0, config.status);
        
        // 显示/隐藏取消按钮
        this.cancelBtn.style.display = config.showCancel ? 'inline-block' : 'none';
        this.okBtn.style.display = 'none';

        // 显示/隐藏进度条
        this.progressBar.parentElement.parentElement.style.display = config.showProgress ? 'block' : 'none';

        this.isVisible = true;
        this.bsModal.show();
    }

    hide() {
        this.isVisible = false;
        this.currentOperation = null;
        this.bsModal.hide();
    }

    updateProgress(percentage, status = null, details = null) {
        if (!this.isVisible) return;

        // 更新进度条
        this.progressBar.style.width = `${percentage}%`;
        this.progressBar.setAttribute('aria-valuenow', percentage);
        this.percentageText.textContent = `${Math.round(percentage)}%`;

        // 更新状态文本
        if (status) {
            this.statusText.textContent = status;
        }

        // 更新详细信息
        if (details) {
            this.detailsContainer.innerHTML = details;
        }

        // 如果达到100%，显示完成状态
        if (percentage >= 100) {
            this.progressBar.classList.remove('progress-bar-animated');
            this.progressBar.classList.add('bg-success');
            this.cancelBtn.style.display = 'none';
            this.okBtn.style.display = 'inline-block';
        }
    }

    showError(message, details = null) {
        this.progressBar.classList.remove('progress-bar-animated');
        this.progressBar.classList.add('bg-danger');
        this.statusText.textContent = message;
        
        if (details) {
            this.detailsContainer.innerHTML = `<div class="text-danger">${details}</div>`;
        }

        this.cancelBtn.style.display = 'none';
        this.okBtn.style.display = 'inline-block';
        this.okBtn.innerHTML = '<i class="bi bi-x-circle me-2"></i>关闭';
        this.okBtn.className = 'btn btn-danger';
    }

    showSuccess(message, details = null) {
        this.updateProgress(100, message, details);
        this.titleElement.innerHTML = '<i class="bi bi-check-circle me-2"></i>操作完成';
    }

    cancel() {
        if (this.currentOperation && this.currentOperation.abort) {
            this.currentOperation.abort();
        }
        this.hide();
    }

    setOperation(operation) {
        this.currentOperation = operation;
    }
}

// 创建全局实例
window.progressModal = new ProgressModal();

// 文件上传进度功能
function uploadFileWithProgress(formElement, options = {}) {
    const formData = new FormData(formElement);
    const xhr = new XMLHttpRequest();

    // 设置操作引用以便取消
    progressModal.setOperation(xhr);

    // 显示进度条
    progressModal.show({
        title: '上传文件',
        titleIcon: 'bi-upload',
        status: '正在上传文件...',
        showCancel: true
    });

    // 监听上传进度
    xhr.upload.addEventListener('progress', function(e) {
        if (e.lengthComputable) {
            const percentage = (e.loaded / e.total) * 100;
            const loaded = formatFileSize(e.loaded);
            const total = formatFileSize(e.total);
            
            progressModal.updateProgress(
                percentage,
                `正在上传... ${loaded} / ${total}`,
                `上传速度: ${calculateSpeed(e.loaded, Date.now() - startTime)}/s`
            );
        }
    });

    // 记录开始时间用于计算速度
    const startTime = Date.now();

    return new Promise((resolve, reject) => {
        xhr.onload = function() {
            if (xhr.status >= 200 && xhr.status < 300) {
                progressModal.showSuccess('文件上传成功！', '文件已成功上传到服务器');
                setTimeout(() => {
                    resolve(xhr.responseText);
                }, 1500);
            } else {
                progressModal.showError('上传失败', `HTTP错误: ${xhr.status}`);
                reject(new Error(`HTTP ${xhr.status}`));
            }
        };

        xhr.onerror = function() {
            progressModal.showError('上传失败', '网络连接错误或服务器无响应');
            reject(new Error('Network error'));
        };

        xhr.onabort = function() {
            progressModal.hide();
            reject(new Error('Upload cancelled'));
        };

        // 发送请求
        xhr.open('POST', formElement.action);
        
        // 添加防伪令牌
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            xhr.setRequestHeader('RequestVerificationToken', token.value);
        }

        xhr.send(formData);
    });
}

// 文件下载进度功能
function downloadFileWithProgress(url, filename) {
    progressModal.show({
        title: '下载文件',
        titleIcon: 'bi-download',
        status: '正在下载文件...',
        showCancel: true
    });

    const xhr = new XMLHttpRequest();
    progressModal.setOperation(xhr);

    return new Promise((resolve, reject) => {
        xhr.open('GET', url);
        xhr.responseType = 'blob';

        xhr.addEventListener('progress', function(e) {
            if (e.lengthComputable) {
                const percentage = (e.loaded / e.total) * 100;
                const loaded = formatFileSize(e.loaded);
                const total = formatFileSize(e.total);
                
                progressModal.updateProgress(
                    percentage,
                    `正在下载... ${loaded} / ${total}`
                );
            }
        });

        xhr.onload = function() {
            if (xhr.status >= 200 && xhr.status < 300) {
                // 创建下载链接
                const blob = xhr.response;
                const downloadUrl = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = downloadUrl;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(downloadUrl);

                progressModal.showSuccess('文件下载完成！', '文件已保存到您的下载文件夹');
                setTimeout(() => {
                    resolve();
                }, 1500);
            } else {
                progressModal.showError('下载失败', `HTTP错误: ${xhr.status}`);
                reject(new Error(`HTTP ${xhr.status}`));
            }
        };

        xhr.onerror = function() {
            progressModal.showError('下载失败', '网络连接错误或服务器无响应');
            reject(new Error('Network error'));
        };

        xhr.onabort = function() {
            progressModal.hide();
            reject(new Error('Download cancelled'));
        };

        xhr.send();
    });
}

// 批量操作进度功能
function performBatchOperationWithProgress(operations, operationType) {
    progressModal.show({
        title: `批量${operationType}`,
        titleIcon: 'bi-gear',
        status: '正在处理...',
        showCancel: false
    });

    let completed = 0;
    const total = operations.length;
    const results = [];

    return new Promise((resolve, reject) => {
        const processNext = async () => {
            if (completed >= total) {
                const successCount = results.filter(r => r.success).length;
                const failCount = results.filter(r => !r.success).length;
                
                progressModal.showSuccess(
                    `批量${operationType}完成！`,
                    `成功: ${successCount}，失败: ${failCount}`
                );
                
                setTimeout(() => {
                    resolve(results);
                }, 1500);
                return;
            }

            const operation = operations[completed];
            const percentage = (completed / total) * 100;
            
            progressModal.updateProgress(
                percentage,
                `正在处理第 ${completed + 1} 项，共 ${total} 项`,
                `当前项目: ${operation.name || '未知'}`
            );

            try {
                const result = await operation.execute();
                results.push({ success: true, result });
            } catch (error) {
                results.push({ success: false, error });
            }

            completed++;
            setTimeout(processNext, 100); // 添加小延迟以显示进度
        };

        processNext();
    });
}

// 工具函数
function formatFileSize(bytes) {
    if (bytes < 1024) {
        return bytes + ' B';
    } else if (bytes < 1024 * 1024) {
        return (bytes / 1024).toFixed(1) + ' KB';
    } else if (bytes < 1024 * 1024 * 1024) {
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    } else {
        return (bytes / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
    }
}

function calculateSpeed(bytes, milliseconds) {
    const seconds = milliseconds / 1000;
    const bytesPerSecond = bytes / seconds;
    return formatFileSize(bytesPerSecond);
} 