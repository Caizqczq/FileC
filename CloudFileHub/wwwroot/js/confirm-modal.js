// 自定义确认模态框组件
class ConfirmModal {
    constructor() {
        this.modal = null;
        this.bsModal = null;
        this.currentResolve = null;
        this.currentReject = null;
        this.init();
    }

    init() {
        // 创建模态框HTML
        const modalHtml = `
            <div class="modal fade" id="confirmModal" tabindex="-1" aria-labelledby="confirmModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header border-0 pb-0">
                            <div class="d-flex align-items-center">
                                <div class="confirm-icon-container me-3" id="confirmIconContainer">
                                    <i class="bi bi-exclamation-triangle text-warning fs-1" id="confirmIcon"></i>
                                </div>
                                <div>
                                    <h5 class="modal-title mb-0" id="confirmModalLabel">确认操作</h5>
                                    <small class="text-muted" id="confirmSubtitle">此操作需要您的确认</small>
                                </div>
                            </div>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body pt-2">
                            <div id="confirmMessage" class="mb-3">
                                确定要执行此操作吗？
                            </div>
                            <div id="confirmDetails" class="small text-muted">
                                <!-- 详细信息将在这里显示 -->
                            </div>
                        </div>
                        <div class="modal-footer border-0 pt-0">
                            <button type="button" class="btn btn-outline-secondary" id="confirmCancelBtn">
                                <i class="bi bi-x-circle me-2"></i>取消
                            </button>
                            <button type="button" class="btn btn-danger" id="confirmOkBtn">
                                <i class="bi bi-check-circle me-2"></i>确认
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // 添加到页面
        if (!document.getElementById('confirmModal')) {
            document.body.insertAdjacentHTML('beforeend', modalHtml);
        }

        // 获取元素引用
        this.modal = document.getElementById('confirmModal');
        this.titleElement = document.getElementById('confirmModalLabel');
        this.subtitleElement = document.getElementById('confirmSubtitle');
        this.messageElement = document.getElementById('confirmMessage');
        this.detailsElement = document.getElementById('confirmDetails');
        this.iconContainer = document.getElementById('confirmIconContainer');
        this.iconElement = document.getElementById('confirmIcon');
        this.cancelBtn = document.getElementById('confirmCancelBtn');
        this.okBtn = document.getElementById('confirmOkBtn');

        // 初始化Bootstrap模态框
        this.bsModal = new bootstrap.Modal(this.modal);

        // 绑定事件
        this.cancelBtn.addEventListener('click', () => {
            this.hide();
            if (this.currentReject) {
                this.currentReject(false);
                this.currentReject = null;
            }
        });

        this.okBtn.addEventListener('click', () => {
            this.hide();
            if (this.currentResolve) {
                this.currentResolve(true);
                this.currentResolve = null;
            }
        });

        // 监听模态框关闭事件
        this.modal.addEventListener('hidden.bs.modal', () => {
            if (this.currentReject) {
                this.currentReject(false);
                this.currentReject = null;
            }
        });
    }

    show(options = {}) {
        return new Promise((resolve, reject) => {
            const defaultOptions = {
                title: '确认操作',
                subtitle: '此操作需要您的确认',
                message: '确定要执行此操作吗？',
                details: '',
                icon: 'bi-exclamation-triangle',
                iconColor: 'text-warning',
                confirmText: '确认',
                confirmClass: 'btn-danger',
                cancelText: '取消',
                type: 'warning'
            };

            const config = { ...defaultOptions, ...options };

            // 设置标题和副标题
            this.titleElement.textContent = config.title;
            this.subtitleElement.textContent = config.subtitle;

            // 设置消息
            this.messageElement.textContent = config.message;

            // 设置详细信息
            if (config.details) {
                this.detailsElement.innerHTML = config.details;
                this.detailsElement.style.display = 'block';
            } else {
                this.detailsElement.style.display = 'none';
            }

            // 设置图标
            this.iconElement.className = `bi ${config.icon} ${config.iconColor} fs-1`;

            // 根据类型设置样式
            this.setModalTheme(config.type);

            // 设置按钮文本和样式
            this.cancelBtn.innerHTML = `<i class="bi bi-x-circle me-2"></i>${config.cancelText}`;
            this.okBtn.innerHTML = `<i class="bi bi-check-circle me-2"></i>${config.confirmText}`;
            this.okBtn.className = `btn ${config.confirmClass}`;

            // 保存回调函数
            this.currentResolve = resolve;
            this.currentReject = reject;

            // 显示模态框
            this.bsModal.show();
        });
    }

    hide() {
        this.bsModal.hide();
    }

    setModalTheme(type) {
        // 移除所有主题类
        this.modal.classList.remove('modal-danger', 'modal-warning', 'modal-info', 'modal-success');
        
        // 添加对应主题类
        if (type) {
            this.modal.classList.add(`modal-${type}`);
        }
    }

    // 预设的确认对话框类型
    static confirmDelete(options = {}) {
        const deleteOptions = {
            title: '确认删除',
            subtitle: '此操作无法撤销',
            message: options.message || '确定要删除此项目吗？',
            details: options.details || '删除后将无法恢复，请谨慎操作。',
            icon: 'bi-trash',
            iconColor: 'text-danger',
            confirmText: '删除',
            confirmClass: 'btn-danger',
            type: 'danger',
            ...options
        };

        return window.confirmModal.show(deleteOptions);
    }

    static confirmAction(options = {}) {
        const actionOptions = {
            title: '确认操作',
            subtitle: '请确认您的操作',
            icon: 'bi-question-circle',
            iconColor: 'text-primary',
            confirmText: '确认',
            confirmClass: 'btn-primary',
            type: 'info',
            ...options
        };

        return window.confirmModal.show(actionOptions);
    }

    static confirmWarning(options = {}) {
        const warningOptions = {
            title: '警告',
            subtitle: '请注意此操作的风险',
            icon: 'bi-exclamation-triangle',
            iconColor: 'text-warning',
            confirmText: '继续',
            confirmClass: 'btn-warning',
            type: 'warning',
            ...options
        };

        return window.confirmModal.show(warningOptions);
    }
}

// 创建全局实例
window.confirmModal = new ConfirmModal();

// 全局confirm函数（覆盖原生confirm）
window.customConfirm = function(message, options = {}) {
    return ConfirmModal.confirmAction({
        message: message,
        ...options
    });
};

// 批量删除确认
window.confirmBatchDelete = function(count, type = '项目') {
    return ConfirmModal.confirmDelete({
        message: `确定要删除选中的 ${count} 个${type}吗？`,
        details: `此操作将永久删除 ${count} 个${type}，删除后无法恢复。`
    });
};

// 文件删除确认
window.confirmFileDelete = function(fileName) {
    return ConfirmModal.confirmDelete({
        message: `确定要删除文件 "${fileName}" 吗？`,
        details: '文件删除后将无法恢复，请确认此操作。'
    });
};

// 文件夹删除确认
window.confirmFolderDelete = function(folderName) {
    return ConfirmModal.confirmDelete({
        message: `确定要删除文件夹 "${folderName}" 吗？`,
        details: '文件夹及其所有内容将被永久删除，此操作无法撤销。'
    });
};

// 分享删除确认
window.confirmShareDelete = function(shareName) {
    return ConfirmModal.confirmDelete({
        message: `确定要删除分享 "${shareName}" 吗？`,
        details: '删除后其他用户将无法再通过此链接访问文件。'
    });
}; 