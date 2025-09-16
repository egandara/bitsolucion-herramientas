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
    let fileStore = new DataTransfer();

    // --- FUNCIÓN HELPER (NUEVA) ---
    function getBadgeClass(severity) {
        if (severity === "Critical") {
            return "badge bg-danger rounded-pill";
        }
        if (severity === "Info") {
            return "badge bg-info rounded-pill";
        }
        // Default es "Warning"
        return "badge bg-warning text-dark rounded-pill";
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
        renderFileList();
    });
    function addFilesToStore(files) {
        if (files.length > 0) {
            for (const file of files) { fileStore.items.add(file); }
            renderFileList();
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
            listItem.textContent = file.name;
            const removeBtn = document.createElement('button');
            removeBtn.className = 'btn btn-danger btn-sm';
            removeBtn.innerHTML = '&times;';
            removeBtn.type = 'button';
            removeBtn.ariaLabel = 'Eliminar ' + file.name;
            removeBtn.addEventListener('click', () => removeFile(i));
            listItem.appendChild(removeBtn);
            fileListUl.appendChild(listItem);
        }
    }

    function removeFile(indexToRemove) {
        const newFiles = new DataTransfer();
        for (let i = 0; i < fileStore.files.length; i++) {
            if (i !== indexToRemove) { newFiles.items.add(fileStore.files[i]); }
        }
        fileStore = newFiles;
        renderFileList();
    }

    // --- LÓGICA DE ANÁLISIS ---
    validateButton.addEventListener('click', function () {
        if (fileStore.files.length === 0) {
            alert('Por favor, selecciona al menos un archivo para analizar.');
            return;
        }

        const spinner = this.querySelector('.spinner-border');
        this.disabled = true;
        spinner.style.display = 'inline-block';
        this.childNodes[2].nodeValue = ' Analizando...';

        const formData = new FormData();
        for (const file of fileStore.files) { formData.append('files', file); }

        document.getElementById('summary-card').style.display = 'none';
        document.getElementById('export-button').style.display = 'none';
        resultsContainer.innerHTML = '<div class="text-center mt-4"><div class="spinner-border" role="status"></div><p>Analizando...</p></div>';


        fetch('/Home/Validate', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if ($.fn.DataTable.isDataTable('#resultsTable')) {
                    $('#resultsTable').DataTable().destroy();
                }

                renderSummary(data.summary);
                renderResultsTable(data.findings);

                if (data.hasResults) {
                    document.getElementById('export-button').style.display = 'inline-block';
                    if (data.findings && data.findings.length > 0 && $.fn.DataTable) {
                        $('#resultsTable').DataTable({
                            language: { url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/es-ES.json' },
                            "pageLength": 10
                        });
                    }
                }
            })
            .catch(error => {
                console.error('Error:', error);
                resultsContainer.innerHTML = '<div class="alert alert-danger">Ocurrió un error al contactar el servidor.</div>';
            })
            .finally(() => {
                this.disabled = false;
                spinner.style.display = 'none';
                this.childNodes[2].nodeValue = ' Analizar Archivos';
            });
    });

    // --- LÓGICA DEL FILTRO INTERACTIVO ---
    summaryContent.addEventListener('click', function (e) {
        const filterTarget = e.target.closest('[data-filter]');

        if (!filterTarget) return;

        const filterValue = filterTarget.dataset.filter;
        const dataTable = $('#resultsTable').DataTable();

        if (dataTable) {
            dataTable.column(1).search(filterValue).draw();
        }
    });


    function renderSummary(summaryData) {
        const summaryCard = document.getElementById('summary-card');
        summaryContent.innerHTML = '';

        const severityOrder = {
            "Critical": 1,
            "Warning": 2,
            "Info": 3
        };

        if (summaryData.hasOwnProperty('Error de Cuota')) {
            summaryContent.innerHTML = `<div class="alert alert-danger m-3">${summaryData['Error de Cuota']}</div>`;
        } else if (Object.keys(summaryData).length === 0) {
            summaryContent.innerHTML = '<div class="alert alert-success m-3">¡Excelente! No se encontraron problemas.</div>';
        } else {

            // --- ORDENAMIENTO (CORREGIDO A camelCase) ---
            const sortedProblems = Object.entries(summaryData).sort((a, b) => {
                const severityA = severityOrder[a[1].severity] || 99; // <-- Corregido a .severity
                const severityB = severityOrder[b[1].severity] || 99; // <-- Corregido a .severity

                if (severityA !== severityB) {
                    return severityA - severityB;
                }
                return a[0].localeCompare(b[0]);
            });

            const ul = document.createElement('ul');
            ul.className = 'list-group list-group-flush';
            let totalProblems = 0;

            // --- BUCLE FOR (CORREGIDO A camelCase) ---
            for (const [problemType, data] of sortedProblems) { // data es { count, severity }

                let badgeClass = getBadgeClass(data.severity); // <-- Corregido a .severity

                const li = document.createElement('li');
                li.className = 'list-group-item d-flex justify-content-between align-items-center';
                li.style.cursor = 'pointer';
                li.dataset.filter = problemType;
                li.title = `Filtrar por "${problemType}"`;

                li.innerHTML = `${problemType} <span class="${badgeClass}">${data.count}</span>`; // <-- Corregido a .count
                ul.appendChild(li);
                totalProblems += data.count; // <-- Corregido a .count
            }

            const totalDiv = document.createElement('div');
            totalDiv.className = 'summary-total';
            totalDiv.style.cursor = 'pointer';
            totalDiv.dataset.filter = '';
            totalDiv.title = 'Clic para limpiar filtro y ver todo';

            totalDiv.innerHTML = `<div class="total-label">Total de Problemas</div><div class="total-value">${totalProblems}</div>`;

            summaryContent.appendChild(ul);
            summaryContent.appendChild(totalDiv);
        }
        summaryCard.style.display = 'flex';
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

            // Esta lógica ya era camelCase y estaba correcta
            let badgeClass = "badge bg-warning text-dark"; // Default
            if (finding.severity === "Critical") {
                badgeClass = "badge bg-danger";
            } else if (finding.severity === "Info") {
                badgeClass = "badge bg-info";
            }

            const tr = document.createElement('tr');
            tr.className = 'clickable-row';
            tr.style.cursor = 'pointer';
            tr.dataset.sourceCode = btoa(finding.cellSourceCode || '');
            tr.dataset.lineNumber = finding.lineNumber || '';

            // Esta lógica ya era camelCase y estaba correcta
            tr.innerHTML = `
                <td>${escapeHtml(finding.fileName || 'N/A')}</td>
                <td><span class="${badgeClass}">${escapeHtml(finding.findingType || 'N/A')}</span></td>
                <td>${finding.cellNumber || 'N/A'}</td>
                <td>${finding.lineNumber || 'N/A'}</td>
                <td><code>${escapeHtml(finding.content || '')}</code></td>
                <td>${escapeHtml(finding.details || 'N/A')}</td>`;
            tbody.appendChild(tr);
        }
        resultsContainer.appendChild(table);
    }

    function escapeHtml(text) {
        const map = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }

    // --- LÓGICA DEL MODAL DE CÓDIGO ---
    const codeModal = new bootstrap.Modal(document.getElementById('codeModal'));
    const codeContentEl = document.getElementById('code-content');
    const codeModalLabel = document.getElementById('codeModalLabel');

    $('#results-container').on('click', '.clickable-row', function () {
        const row = $(this);
        const sourceCode = atob(row.data('source-code'));
        const lineNumber = parseInt(row.data('line-number'));
        const fileName = row.find('td:eq(0)').text();
        const findingType = row.find('td:eq(1)').text();

        codeModalLabel.textContent = `Detalle de "${findingType}" en ${fileName}`;

        const highlightedHtml = Prism.highlight(sourceCode, Prism.languages.python, 'python');
        const lines = highlightedHtml.split('\n');

        if (lineNumber > 0 && lineNumber <= lines.length) {
            lines[lineNumber - 1] = `<span class="line-highlight">${lines[lineNumber - 1]}</span>`;
        }

        codeContentEl.innerHTML = lines.join('\n');
        codeModal.show();
    });
});