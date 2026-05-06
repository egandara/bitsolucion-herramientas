document.addEventListener('DOMContentLoaded', function () {
    // Referencias a elementos del DOM
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

    // Inicializar estado
    updateControls();

    // Helper para tamaño legible
    function humanFileSize(bytes) {
        if (bytes === 0) return '0 B';
        const units = ['B', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return (bytes / Math.pow(1024, i)).toFixed(i === 0 ? 0 : 2) + ' ' + units[i];
    }

    // --- FUNCIÓN HELPER (NUEVA) ---
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

    // --- LÓGICA DE MANEJO DE ARCHIVOS ---
    fileInput.addEventListener('change', e => addFilesToStore(e.target.files));
    uploadCard.addEventListener('dragover', e => {
        e.preventDefault();
        uploadCard.classList.add('drag-over');
    });
    uploadCard.addEventListener('dragleave', () => uploadCard.classList.remove('drag-over'));
    uploadCard.addEventListener('drop', e => {
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
                // Evitar duplicados por nombre y tamaño
                let exists = false;
                for (let i = 0; i < fileStore.files.length; i++) {
                    if (file.name === fileStore.files[i].name && file.size === fileStore.files[i].size) {
                        exists = true;
                        break;
                    }
                }
                if (!exists) fileStore.items.add(file);
            }
            // Render y actualización de badges
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
            listItem.className = 'list-group-item d-flex justify-content-between align-items-center';

            // LEFT: icon + name (flexible)
            const leftDiv = document.createElement('div');
            leftDiv.className = 'file-left'; // aplicado para css de truncado

            // Icono por tipo
            const ext = file.name.split('.').pop()?.toLowerCase();
            const icon = document.createElement('i');
            icon.className = 'me-2';
            if (ext === 'py') icon.className += ' bi bi-file-code text-secondary';
            else if (ext === 'ipynb') icon.className += ' bi bi-file-earmark-text text-primary';
            else if (ext === 'zip') icon.className += ' bi bi-file-zip-fill text-warning';
            else icon.className += ' bi bi-file-earmark';

            const nameSpan = document.createElement('span');
            nameSpan.className = 'file-name'; // para aplicar ellipsis
            nameSpan.textContent = file.name;
            nameSpan.title = file.name;

            leftDiv.appendChild(icon);
            leftDiv.appendChild(nameSpan);

            // RIGHT: size + remove (fijo)
            const rightDiv = document.createElement('div');
            rightDiv.className = 'file-right'; // clase para controlar tamaño fijo

            const sizeSpan = document.createElement('small');
            sizeSpan.className = 'text-muted';
            sizeSpan.textContent = humanFileSize(file.size);

            const removeBtn = document.createElement('button');
            removeBtn.className = 'btn btn-danger btn-sm';
            removeBtn.innerHTML = '&times;';
            removeBtn.type = 'button';
            removeBtn.setAttribute('aria-label', `Eliminar ${file.name}`);
            removeBtn.addEventListener('click', () => removeFile(i));

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
            if (i !== indexToRemove) { newFiles.items.add(fileStore.files[i]); }
        }
        fileStore = newFiles;
        fileInput.value = '';
        renderFileList();
        updateSummaryBadges();
    }

    // --- LÓGICA DE ANÁLISIS ---
    validateButton.addEventListener('click', function () {
        if (fileStore.files.length === 0) {
            return;
        }

        const spinner = this.querySelector('.spinner-border');
        this.disabled = true;
        if (spinner) spinner.style.display = 'inline-block';
        this.childNodes[2].nodeValue = ' Analizando...';

        const formData = new FormData();
        for (const file of fileStore.files) { formData.append('files', file); }

        document.getElementById('summary-card').style.display = 'none';
        document.getElementById('export-button').style.display = 'none';
        resultsContainer.innerHTML = '<div class="text-center mt-4"><div class="spinner-border" role="status"></div><p>Analizando...</p></div>';

        // Nueva versión robusta de fetch: maneja respuestas no-json y códigos HTTP distintos de 200
        fetch('/Home/Validate', {
            method: 'POST',
            body: formData
        })
            .then(async response => {
                const contentType = response.headers.get('content-type') || '';
                if (!response.ok) {
                    // Intentar leer cuerpo: JSON con campo error o texto puro
                    try {
                        if (contentType.includes('application/json')) {
                            const json = await response.json();
                            const err = json && json.error ? json.error : JSON.stringify(json);
                            throw new Error(err || `HTTP ${response.status}`);
                        } else {
                            const text = await response.text();
                            throw new Error(text || `HTTP ${response.status}`);
                        }
                    } catch (readErr) {
                        // Si no se pudo parsear, devolver un mensaje genérico con status
                        throw new Error(readErr.message || `HTTP ${response.status}`);
                    }
                }

                // OK: parsear JSON si viene; si no, tratar como error
                if (contentType.includes('application/json')) {
                    return response.json();
                } else {
                    const text = await response.text();
                    throw new Error(text || 'Respuesta inesperada del servidor.');
                }
            })
            .then(data => {
                if (!data) {
                    resultsContainer.innerHTML = '<div class="alert alert-danger">Respuesta vacía del servidor.</div>';
                    return;
                }

                if (data.error) {
                    resultsContainer.innerHTML = `<div class="alert alert-danger">${escapeHtml(data.error)}</div>`;
                    return;
                }

                // Limpiar mensajes previos
                resultsContainer.innerHTML = '';

                if (window.jQuery && $.fn.DataTable && $.fn.DataTable.isDataTable && $.fn.DataTable.isDataTable('#resultsTable')) {
                    $('#resultsTable').DataTable().destroy();
                }

                renderSummary(data.summary);
                renderResultsTable(data.findings);

                if (data.hasResults) {
                    document.getElementById('export-button').style.display = 'inline-block';
                    if (data.findings && data.findings.length > 0 && window.jQuery && $.fn.DataTable) {
                        $('#resultsTable').DataTable({
                            language: { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
                            pageLength: 10
                        });
                    }
                }
            })
            .catch(error => {
                console.error('Fetch error:', error);
                // Mostrar el mensaje del servidor si existe, sino un mensaje legible
                const msg = error && error.message ? error.message : 'Ocurrió un error al contactar el servidor.';
                resultsContainer.innerHTML = `<div class="alert alert-danger">${escapeHtml(msg)}</div>`;
            })
            .finally(() => {
                this.disabled = false;
                const spinner = this.querySelector('.spinner-border');
                if (spinner) spinner.style.display = 'none';
                this.childNodes[2].nodeValue = ' Analizar Archivos';
                updateControls();
            });
    });

    // --- LÓGICA DEL FILTRO INTERACTIVO ---
    summaryContent.addEventListener('click', function (e) {
        const filterTarget = e.target.closest('[data-filter]');

        if (!filterTarget) return;

        const filterValue = filterTarget.dataset.filter;
        if (window.jQuery && $.fn.DataTable) {
            const dataTable = $('#resultsTable').DataTable();
            if (dataTable) {
                dataTable.column(1).search(filterValue).draw();
            }
        }
    });

    function escapeHtml(text) {
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(text || '').replace(/[&<>"']/g, m => map[m]);
    }

    // --- RENDER: resumen y tabla (añadido para evitar "renderSummary is not defined") ---
    function renderSummary(summaryData) {
        const summaryCard = document.getElementById('summary-card');
        summaryContent.innerHTML = '';

        if (!summaryData || Object.keys(summaryData).length === 0) {
            summaryContent.innerHTML = '<div class="alert alert-success m-3">¡Excelente! No se encontraron problemas.</div>';
            if (summaryCard) summaryCard.style.display = 'flex';
            return;
        }

        if (summaryData['Error de Cuota']) {
            summaryContent.innerHTML = `<div class="alert alert-danger m-3">${escapeHtml(summaryData['Error de Cuota'])}</div>`;
            if (summaryCard) summaryCard.style.display = 'flex';
            return;
        }

        const severityOrder = { "Critical": 1, "Warning": 2, "Info": 3 };

        // summaryData may be { type: { Count, Severity } } or { type: { count, severity } }
        const entries = Object.entries(summaryData).map(([k, v]) => {
            const count = v.Count ?? v.count ?? 0;
            const severity = v.Severity ?? v.severity ?? 'Info';
            return { problemType: k, count, severity };
        });

        entries.sort((a, b) => {
            const sa = severityOrder[a.severity] || 99;
            const sb = severityOrder[b.severity] || 99;
            if (sa !== sb) return sa - sb;
            return a.problemType.localeCompare(b.problemType);
        });

        const ul = document.createElement('ul');
        ul.className = 'list-group list-group-flush';
        let totalProblems = 0;

        for (const item of entries) {
            const li = document.createElement('li');
            li.className = 'list-group-item d-flex justify-content-between align-items-center';
            li.style.cursor = 'pointer';
            li.dataset.filter = item.problemType;
            li.title = `Filtrar por "${item.problemType}"`;

            const badgeClass = (item.severity === 'Critical') ? 'badge bg-danger rounded-pill' :
                (item.severity === 'Info') ? 'badge bg-info rounded-pill' : 'badge bg-warning text-dark rounded-pill';

            li.innerHTML = `${escapeHtml(item.problemType)} <span class="${badgeClass}">${item.count}</span>`;
            ul.appendChild(li);
            totalProblems += item.count;
        }

        const totalDiv = document.createElement('div');
        totalDiv.className = 'summary-total mt-2';
        totalDiv.style.cursor = 'pointer';
        totalDiv.dataset.filter = '';
        totalDiv.title = 'Clic para limpiar filtro y ver todo';
        totalDiv.innerHTML = `<div class="total-label">Total de Problemas</div><div class="total-value">${totalProblems}</div>`;

        summaryContent.appendChild(ul);
        summaryContent.appendChild(totalDiv);
        if (summaryCard) summaryCard.style.display = 'flex';
    }

    function renderResultsTable(findings) {
        resultsContainer.innerHTML = '';
        if (!findings || findings.length === 0) {
            if (!document.querySelector('#summary-content .alert-danger')) {
                resultsContainer.innerHTML = '<div class="alert alert-success mt-3">No se encontraron problemas en los archivos analizados.</div>';
            }
            return;
        }

        const table = document.createElement('table');
        table.id = 'resultsTable';
        table.className = 'table table-bordered table-striped mt-3';
        table.innerHTML = `<thead class="thead-dark"><tr><th>Archivo</th><th>Tipo de Problema</th><th>Celda</th><th>Línea</th><th>Contenido</th><th>Detalle</th></tr></thead><tbody></tbody>`;

        const tbody = table.querySelector('tbody');

        for (const finding of findings) {
            const badgeClass = finding.Severity === "Critical" || finding.severity === "Critical" ? "badge bg-danger" :
                (finding.Severity === "Info" || finding.severity === "Info") ? "badge bg-info" : "badge bg-warning text-dark";

            const tr = document.createElement('tr');
            tr.className = 'clickable-row';
            tr.style.cursor = 'pointer';

            // Keep compatibility with different property casing
            const fileName = finding.FileName ?? finding.fileName ?? 'N/A';
            const findingType = finding.FindingType ?? finding.findingType ?? 'N/A';
            const cellNumber = finding.CellNumber ?? finding.cellNumber ?? 'N/A';
            const lineNumber = finding.LineNumber ?? finding.lineNumber ?? 'N/A';
            const content = finding.Content ?? finding.content ?? '';
            const details = finding.Details ?? finding.details ?? '';

            // Store source code (if present) base64 encoded for modal use
            const sourceCode = finding.CellSourceCode ?? finding.cellSourceCode ?? '';
            tr.dataset.sourceCode = btoa(unescape(encodeURIComponent(sourceCode || '')));
            tr.dataset.lineNumber = lineNumber;

            tr.innerHTML = `
                <td>${escapeHtml(fileName)}</td>
                <td><span class="${badgeClass}">${escapeHtml(findingType)}</span></td>
                <td>${escapeHtml(cellNumber?.toString?.() ?? 'N/A')}</td>
                <td>${escapeHtml(lineNumber?.toString?.() ?? 'N/A')}</td>
                <td><code>${escapeHtml(content)}</code></td>
                <td>${escapeHtml(details)}</td>`;
            tbody.appendChild(tr);
        }

        resultsContainer.appendChild(table);

        // Delegated click handler to open modal with code (if Prism and bootstrap exist)
        resultsContainer.querySelectorAll('table#resultsTable tbody tr.clickable-row').forEach(tr => {
            tr.addEventListener('click', function () {
                const encoded = this.dataset.sourceCode || '';
                let source = '';
                try {
                    source = decodeURIComponent(escape(atob(encoded || '')));
                } catch { source = ''; }

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

    // Las funciones renderSummary, renderResultsTable quedan igual que antes.
});
