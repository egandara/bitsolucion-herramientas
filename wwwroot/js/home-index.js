document.addEventListener('DOMContentLoaded', function () {
    // --- TU LÓGICA ORIGINAL INTACTA ---
    const fileInput = document.getElementById('file-input');
    const validateButton = document.getElementById('validate-button');
    const clearFilesButton = document.getElementById('clear-files-button');
    const fileListUl = document.getElementById('file-list');
    const emptyListItem = document.getElementById('empty-list-item');
    const resultsContainer = document.getElementById('results-container');
    const uploadCard = document.getElementById('upload-card');
    const summaryContent = document.getElementById('summary-content');
    const selectedCountEl = document.getElementById('selected-count');
    const totalSizeEl = document.getElementById('total-size');

    let fileStore = new DataTransfer();
    window.detectedVars = {};
    window.typesToClean = [];

    updateControls();

    function humanFileSize(bytes) {
        if (bytes === 0) return '0 B';
        const units = ['B', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return (bytes / Math.pow(1024, i)).toFixed(i === 0 ? 0 : 2) + ' ' + units[i];
    }

    function updateSummaryBadges() {
        const count = fileStore.files.length;
        let total = 0;
        for (let i = 0; i < fileStore.files.length; i++) {
            total += fileStore.files[i].size;
        }
        selectedCountEl.textContent = `${count} archivo${count !== 1 ? 's' : ''}`;
        totalSizeEl.textContent = humanFileSize(total);
        updateControls();
    }

    function updateControls() {
        const hasFiles = fileStore.files.length > 0;
        validateButton.disabled = !hasFiles;
        validateButton.setAttribute('aria-disabled', String(!hasFiles));
        clearFilesButton.style.display = hasFiles ? 'inline-block' : 'none';
    }

    fileInput.addEventListener('change', function (e) {
        addFilesToStore(e.target.files);
    });

    uploadCard.addEventListener('dragover', function (e) {
        e.preventDefault();
        uploadCard.classList.add('drag-over');
    });

    uploadCard.addEventListener('dragleave', function () {
        uploadCard.classList.remove('drag-over');
    });

    uploadCard.addEventListener('drop', function (e) {
        e.preventDefault();
        uploadCard.classList.remove('drag-over');
        addFilesToStore(e.dataTransfer.files);
    });

    clearFilesButton.addEventListener('click', function () {
        fileStore = new DataTransfer();
        fileInput.value = '';
        renderFileList();
        updateSummaryBadges();
    });

    function addFilesToStore(files) {
        if (files.length > 0) {
            for (const file of files) {
                let exists = false;
                for (let i = 0; i < fileStore.files.length; i++) {
                    if (file.name === fileStore.files[i].name && file.size === fileStore.files[i].size) {
                        exists = true;
                        break;
                    }
                }
                if (!exists) {
                    fileStore.items.add(file);
                }
            }
            renderFileList();
            updateSummaryBadges();
        }
    }

    function renderFileList() {
        fileListUl.innerHTML = '';
        if (fileStore.files.length === 0) {
            fileListUl.appendChild(emptyListItem);
            return;
        }

        for (let i = 0; i < fileStore.files.length; i++) {
            const file = fileStore.files[i];
            const listItem = document.createElement('li');
            listItem.className = 'list-group-item d-flex justify-content-between align-items-center bg-dark text-white border-secondary mb-1';

            const leftDiv = document.createElement('div');
            leftDiv.className = 'file-left';

            const ext = file.name.split('.').pop()?.toLowerCase();
            const icon = document.createElement('i');
            icon.className = 'me-2';
            if (ext === 'py') icon.className += ' bi bi-file-code text-secondary';
            else if (ext === 'ipynb') icon.className += ' bi bi-file-earmark-text text-primary';
            else if (ext === 'zip') icon.className += ' bi bi-file-zip-fill text-warning';
            else icon.className += ' bi bi-file-earmark';

            const nameSpan = document.createElement('span');
            nameSpan.className = 'file-name';
            nameSpan.textContent = file.name;
            nameSpan.title = file.name;

            leftDiv.appendChild(icon);
            leftDiv.appendChild(nameSpan);

            const rightDiv = document.createElement('div');
            rightDiv.className = 'file-right';

            const sizeSpan = document.createElement('small');
            sizeSpan.className = 'text-muted';
            sizeSpan.textContent = humanFileSize(file.size);

            const removeBtn = document.createElement('button');
            removeBtn.className = 'btn btn-danger btn-sm';
            removeBtn.innerHTML = '&times;';
            removeBtn.type = 'button';
            removeBtn.addEventListener('click', function () {
                removeFile(i);
            });

            rightDiv.appendChild(sizeSpan);
            rightDiv.appendChild(removeBtn);

            listItem.appendChild(leftDiv);
            listItem.appendChild(rightDiv);
            fileListUl.appendChild(listItem);
        }
    }

    function removeFile(indexToRemove) {
        const newFiles = new DataTransfer();
        for (let i = 0; i < fileStore.files.length; i++) {
            if (i !== indexToRemove) {
                newFiles.items.add(fileStore.files[i]);
            }
        }
        fileStore = newFiles;
        fileInput.value = '';
        renderFileList();
        updateSummaryBadges();
    }

    validateButton.addEventListener('click', function () {
        if (fileStore.files.length === 0) return;

        const spinner = this.querySelector('.spinner-border');
        this.disabled = true;
        if (spinner) spinner.style.display = 'inline-block';
        this.childNodes[2].nodeValue = ' Analizando...';

        const formData = new FormData();
        for (const file of fileStore.files) {
            formData.append('files', file);
        }

        document.getElementById('summary-card').style.display = 'none';
        document.getElementById('export-button').style.display = 'none';

        const bulkBtn = document.getElementById('btn-bulk-fix');
        if (bulkBtn) bulkBtn.style.display = 'none';

        resultsContainer.innerHTML = `
            <div class="text-center mt-4">
                <div class="spinner-border" role="status"></div>
                <p>Analizando notebooks...</p>
            </div>
        `;

        fetch('/Home/Validate', {
            method: 'POST',
            body: formData
        })
            .then(async response => {
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                return response.json();
            })
            .then(data => {
                resultsContainer.innerHTML = '';
                if (window.jQuery && $.fn.DataTable && $.fn.DataTable.isDataTable('#resultsTable')) {
                    $('#resultsTable').DataTable().destroy();
                }

                window.detectedVars = data.fileVariables;

                renderSummary(data.summary);
                renderResultsTable(data.findings);

                if (data.hasResults) {
                    document.getElementById('export-button').style.display = 'inline-block';

                    const hasCleanable = data.findings.some(f =>
                        f.findingType.includes("Librerias") || f.findingType.includes("Widgets")
                    );
                    if (hasCleanable && bulkBtn) bulkBtn.style.display = 'inline-block';

                    $('#resultsTable').DataTable({
                        language: { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
                        pageLength: 10,
                        order: [[0, 'asc']]
                    });

                    setupClickableRows();
                    setupSummaryFiltering();
                }
            })
            .catch(error => {
                resultsContainer.innerHTML = `<div class="alert alert-danger">${escapeHtml(error.message)}</div>`;
            })
            .finally(() => {
                this.disabled = false;
                const spinner = this.querySelector('.spinner-border');
                if (spinner) spinner.style.display = 'none';
                this.childNodes[2].nodeValue = ' Analizar Archivos';
                updateControls();
            });
    });

    // --- INTEGRACIÓN: RENDER SUMMARY ---
    function renderSummary(summaryData) {
        const summaryCard = document.getElementById('summary-card');
        summaryContent.innerHTML = '';
        window.typesToClean = [];

        if (!summaryData || Object.keys(summaryData).length === 0) {
            summaryContent.innerHTML = '<div class="alert alert-success m-3">No se encontraron problemas.</div>';
            if (summaryCard) summaryCard.style.display = 'flex';
            return;
        }

        const entries = Object.entries(summaryData).map(([k, v]) => ({
            problemType: k,
            count: v.Count ?? v.count ?? 0,
            severity: v.Severity ?? v.severity ?? 'Info'
        }));

        const ul = document.createElement('ul');
        ul.className = 'list-group list-group-flush';

        let totalProblems = 0;
        for (const item of entries) {
            const li = document.createElement('li');
            li.className = 'list-group-item d-flex justify-content-between align-items-center summary-filter-item bg-transparent text-white border-secondary';
            li.style.cursor = 'pointer';
            li.dataset.filter = item.problemType;

            const badgeClass = (item.severity === 'Critical') ? 'badge bg-danger rounded-pill me-2' :
                (item.severity === 'Info') ? 'badge bg-info rounded-pill me-2' :
                    'badge bg-warning text-dark rounded-pill me-2';

            const leftPart = document.createElement('div');
            leftPart.innerHTML = escapeHtml(item.problemType);

            const rightPart = document.createElement('div');
            rightPart.className = 'd-flex align-items-center';

            const countBadge = document.createElement('span');
            countBadge.className = badgeClass;
            countBadge.textContent = item.count;

            // BOTÓN LIMPIAR / CORREGIR
            const cleanBtn = document.createElement('button');
            cleanBtn.className = 'btn btn-sm btn-outline-danger py-0 px-2 ms-2';
            cleanBtn.innerHTML = item.problemType.includes('Incorrecto') ? '<i class="bi bi-magic"></i> Corregir' : '<i class="bi bi-eraser-fill"></i> Limpiar';
            cleanBtn.onclick = (e) => {
                e.stopPropagation();
                toggleCleanCategory(item.problemType, cleanBtn, countBadge);
            };

            rightPart.appendChild(countBadge);
            rightPart.appendChild(cleanBtn);

            li.appendChild(leftPart);
            li.appendChild(rightPart);
            ul.appendChild(li);
            totalProblems += item.count;
        }

        const totalDiv = document.createElement('div');
        totalDiv.className = 'summary-total mt-2';
        totalDiv.innerHTML = `
            <div class="total-label text-light-muted">Total de Problemas</div>
            <div class="total-value text-warning fw-bold fs-3">${totalProblems}</div>
        `;

        summaryContent.appendChild(ul);
        summaryContent.appendChild(totalDiv);
        if (summaryCard) summaryCard.style.display = 'flex';

        const downloadBtn = document.getElementById('btn-download-fixed');
        if (downloadBtn) downloadBtn.style.display = 'inline-block';
    }

    function toggleCleanCategory(type, btn, badge) {
        const idx = window.typesToClean.indexOf(type);
        if (idx === -1) {
            window.typesToClean.push(type);
            btn.classList.replace('btn-outline-danger', 'btn-danger');
            btn.innerHTML = '<i class="bi bi-check-circle-fill"></i> Marcado';
            badge.style.textDecoration = 'line-through';
            badge.style.opacity = '0.5';
        } else {
            window.typesToClean.splice(idx, 1);
            btn.classList.replace('btn-danger', 'btn-outline-danger');
            btn.innerHTML = type.includes('Incorrecto') ? '<i class="bi bi-magic"></i> Corregir' : '<i class="bi bi-eraser-fill"></i> Limpiar';
            badge.style.textDecoration = 'none';
            badge.style.opacity = '1';
        }
    }

    // --- INTEGRACIÓN: DESCARGA ---
    window.downloadCorrectedNotebook = function () {
        if (window.typesToClean.length === 0) {
            if (typeof toastr !== 'undefined') toastr.warning("Selecciona al menos una categoría para procesar.");
            return;
        }

        const formData = new FormData();
        for (const file of fileStore.files) { formData.append('files', file); }
        formData.append('typesToCleanJson', JSON.stringify(window.typesToClean));

        if (typeof toastr !== 'undefined') toastr.info("Procesando archivos corregidos...");

        fetch('/Home/GenerateCorrected', { method: 'POST', body: formData })
            .then(res => res.blob())
            .then(blob => {
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `BitSolucion_Estandarizado_${Date.now()}.zip`;
                a.click();
                if (typeof toastr !== 'undefined') toastr.success("Estandarización completada.");
            })
            .catch(err => { if (typeof toastr !== 'undefined') toastr.error("Error al procesar descarga."); });
    };

    function renderResultsTable(findings) {
        resultsContainer.innerHTML = '';
        if (!findings || findings.length === 0) return;

        const table = document.createElement('table');
        table.id = 'resultsTable';
        table.className = 'table table-bordered table-striped mt-3 bg-dark text-white border-secondary';
        table.innerHTML = `
            <thead class="thead-dark">
                <tr>
                    <th>Archivo</th>
                    <th>Tipo de Problema</th>
                    <th>Celda</th>
                    <th>Línea</th>
                    <th>Contenido</th>
                    <th>Detalle</th>
                    <th class="text-center">Acciones</th>
                </tr>
            </thead>
            <tbody></tbody>
        `;

        const tbody = table.querySelector('tbody');
        for (const finding of findings) {
            const tr = document.createElement('tr');
            tr.className = 'clickable-row';
            tr.style.cursor = 'pointer';

            const sourceCode = finding.CellSourceCode ?? finding.cellSourceCode ?? '';
            tr.dataset.sourceCode = btoa(unescape(encodeURIComponent(sourceCode || '')));
            tr.dataset.lineNumber = finding.LineNumber ?? finding.lineNumber ?? 0;

            const badgeClass = (finding.Severity === "Critical" || finding.severity === "Critical") ? "badge bg-danger" :
                (finding.Severity === "Info" || finding.severity === "Info") ? "badge bg-info" :
                    "badge bg-warning text-dark";

            const fileName = finding.FileName ?? finding.fileName ?? 'N/A';
            const type = finding.FindingType ?? finding.findingType ?? 'N/A';
            const content = finding.Content ?? finding.content ?? '';
            const details = finding.Details ?? finding.details ?? '';

            let actionHtml = '';
            if (type.toUpperCase().includes("SQL")) {
                actionHtml = `<button class="btn btn-sm btn-outline-info" onclick="event.stopPropagation(); window.openSmartFix('${fileName}', '${tr.dataset.sourceCode}')"><i class="bi bi-magic"></i></button>`;
            }

            tr.innerHTML = `
                <td>${escapeHtml(fileName)}</td>
                <td><span class="${badgeClass}">${escapeHtml(type)}</span></td>
                <td>${escapeHtml(finding.CellNumber?.toString() ?? 'N/A')}</td>
                <td>${escapeHtml(finding.LineNumber?.toString() ?? 'N/A')}</td>
                <td><code>${escapeHtml(content)}</code></td>
                <td>${escapeHtml(details)}</td>
                <td class="text-center">${actionHtml}</td>
            `;
            tbody.appendChild(tr);
        }
        resultsContainer.appendChild(table);
    }

    function setupClickableRows() {
        resultsContainer.querySelectorAll('tr.clickable-row').forEach(tr => {
            tr.addEventListener('click', function () {
                let source = '';
                try {
                    source = decodeURIComponent(escape(atob(this.dataset.sourceCode || '')));
                } catch (e) { source = ''; }

                const lineNumber = parseInt(this.dataset.lineNumber) || 0;
                const fileName = this.cells[0].innerText || '';
                const findingType = this.cells[1].innerText || '';

                const codeModal = document.getElementById('codeModal');
                const codeContentEl = document.getElementById('code-content');
                const codeModalLabel = document.getElementById('codeModalLabel');
                if (codeModalLabel) codeModalLabel.textContent = `Detalle de "${findingType}" en ${fileName}`;

                if (window.Prism) {
                    const highlightedHtml = Prism.highlight(source, Prism.languages.python, 'python');
                    const lines = highlightedHtml.split('\n');
                    if (lineNumber > 0 && lineNumber <= lines.length) {
                        lines[lineNumber - 1] = `<span class="line-highlight">${lines[lineNumber - 1]}</span>`;
                    }
                    if (codeContentEl) codeContentEl.innerHTML = lines.join('\n');
                } else {
                    if (codeContentEl) codeContentEl.textContent = source;
                }

                if (window.bootstrap && codeModal) {
                    const modal = new bootstrap.Modal(codeModal);
                    modal.show();
                }
            });
        });
    }

    function setupSummaryFiltering() {
        const filterItems = document.querySelectorAll('.summary-filter-item');
        filterItems.forEach(item => {
            item.addEventListener('click', function () {
                const filterValue = this.dataset.filter;
                if (window.jQuery && $.fn.DataTable && $.fn.DataTable.isDataTable('#resultsTable')) {
                    $('#resultsTable').DataTable().column(1).search(filterValue).draw();
                }
            });
        });
    }

    function escapeHtml(text) {
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(text || '').replace(/[&<>"']/g, m => map[m]);
    }
});

// --- FUNCIONES GLOBALES SMART FIX ---
window.openSmartFix = function (fileName, sourceBase64) {
    const fullCellContent = decodeURIComponent(escape(atob(sourceBase64)));
    document.getElementById('fix-original-code').innerText = fullCellContent;

    let sqlContent = fullCellContent.replace(/^%sql\s*/i, '').trim();

    const ddlRegex = /(DROP TABLE|CREATE TABLE|CREATE OR REPLACE TABLE|CREATE VIEW|CREATE OR REPLACE TEMPORARY VIEW|INSERT INTO|DELETE FROM|UPDATE|MERGE INTO|ALTER TABLE)\s+(?:IF\s+EXISTS\s+)?([\w\.]+)/i;
    const match = sqlContent.match(ddlRegex);

    const previewCodeEl = document.getElementById('fix-preview-code');
    const select = document.getElementById('fix-var-select');
    const stepInput = document.getElementById('fix-step-name');
    const applyBtn = document.getElementById('btn-apply-fix');
    const previewLabel = document.getElementById('preview-label');

    if (!match) {
        stepInput.value = "N/A - Consulta informativa";
        previewCodeEl.innerText = "# Esta celda contiene una consulta informativa (SELECT).\n# Según el estándar del proceso, no se requiere envolver en sqlSafe ni parametrizar.";
        previewCodeEl.style.color = "#adb5bd";
        previewLabel.innerText = "NOTA DE ANÁLISIS:";
        previewLabel.className = "form-label small text-secondary fw-bold";
        applyBtn.disabled = true;
        select.disabled = true;
        new bootstrap.Modal(document.getElementById('smartFixModal')).show();
        return;
    }

    applyBtn.disabled = false;
    select.disabled = false;
    previewLabel.innerText = "NUEVO CÓDIGO ESTANDARIZADO:";
    previewLabel.className = "form-label small text-success fw-bold";
    previewCodeEl.style.color = "#fff";

    const operation = match[1].toUpperCase();
    const fullTableName = match[2];
    const tableName = fullTableName.split('.').pop().toLowerCase();

    let prefix = "paso_";
    if (operation.includes("DROP")) prefix += "drop_";
    else if (operation.includes("CREATE")) prefix += "creacion_";
    else if (operation.includes("INSERT")) prefix += "insert_";
    else prefix += "op_";

    const varNameSugerida = `${prefix}${tableName}`;
    stepInput.value = varNameSugerida;

    select.innerHTML = '<option value="">-- Seleccionar Variable DB --</option>';
    const vars = window.detectedVars[fileName] || [];
    vars.forEach(v => {
        select.innerHTML += `<option value="${v}">${v}</option>`;
    });

    select.onchange = () => {
        const dbVar = select.value || '[VARIABLE_X]';
        let standardizedSql = sqlContent.replace(fullTableName, `""" + ${dbVar} + ".${tableName}"""`);
        const finalCode = `${varNameSugerida} = """${standardizedSql}"""\nsqlSafe(${varNameSugerida})`;
        previewCodeEl.innerText = finalCode;
    };

    select.onchange();
    new bootstrap.Modal(document.getElementById('smartFixModal')).show();
};

window.confirmFix = function () {
    if (typeof toastr !== 'undefined') {
        toastr.success("Corrección aplicada al portapapeles.");
    }
    bootstrap.Modal.getInstance(document.getElementById('smartFixModal')).hide();
};

window.applyBulkFix = function () {
    if (confirm("¿Deseas eliminar automáticamente las librerías y widgets no usados del notebook?")) {
        if (typeof toastr !== 'undefined') {
            toastr.info("Limpiando notebook...");
        }
    }
};
