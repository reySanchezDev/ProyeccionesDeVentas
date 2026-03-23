// Exportaciones XLS para la vista de Gestion Humana.
(function () {
  var CONNECTIVITY_REDIRECT_MARKER = "__HE_REDIRECTING__";
  var btnMarcadas = document.getElementById("btn-gh-export-marcadas");
  var btnConsolidado = document.getElementById("btn-gh-export-consolidado");
  if (!btnMarcadas && !btnConsolidado) {
    return;
  }

  var feedback = document.getElementById("gh-export-feedback");
  var timerId = 0;

  function setStatus(message, cssClass, autoHideMs) {
    if (!feedback) {
      return;
    }

    if (timerId) {
      window.clearTimeout(timerId);
      timerId = 0;
    }

    feedback.classList.remove("export-status-loading", "export-status-ok", "export-status-error");
    var text = (message || "").trim();
    if (!text) {
      feedback.hidden = true;
      feedback.textContent = "";
      return;
    }

    feedback.hidden = false;
    feedback.textContent = text;
    if (cssClass) {
      feedback.classList.add(cssClass);
    }

    var delay = Number.parseInt(autoHideMs, 10);
    if (Number.isFinite(delay) && delay > 0) {
      timerId = window.setTimeout(function () {
        feedback.hidden = true;
        feedback.textContent = "";
        feedback.classList.remove("export-status-loading", "export-status-ok", "export-status-error");
        timerId = 0;
      }, delay);
    }
  }

  function toggleButtons(disabled) {
    if (btnMarcadas) {
      btnMarcadas.disabled = !!disabled;
    }
    if (btnConsolidado) {
      btnConsolidado.disabled = !!disabled;
    }
  }

  function normalizeText(value) {
    return String(value || "").trim();
  }

  function normalizeNumber(value) {
    var parsed = Number.parseFloat(String(value || "").replace(",", "."));
    return Number.isFinite(parsed) ? parsed : 0;
  }

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function formatNumber(value, digits) {
    var decimals = Number.isInteger(digits) && digits >= 0 ? digits : 2;
    return normalizeNumber(value).toFixed(decimals);
  }

  function maybeRedirectForConnectivity(response, payload, error) {
    var bridge = window.HorasExtrasConnectivity;
    if (!bridge || typeof bridge.maybeRedirectForConnectivity !== "function") {
      return false;
    }

    return bridge.maybeRedirectForConnectivity(response, payload, error);
  }

  function getFilterValues() {
    var empleado = document.getElementById("EmpleadoFiltro");
    var fechaI = document.getElementById("FechaEntradaInicio");
    var fechaF = document.getElementById("FechaEntradaFin");
    var fechaRango = document.getElementById("FechaEntradaRango") || document.getElementById("fecha-rango-filtro");

    return {
      empleadoFiltro: empleado ? normalizeText(empleado.value) : "",
      fechaEntradaInicio: fechaI ? normalizeText(fechaI.value) : "",
      fechaEntradaFin: fechaF ? normalizeText(fechaF.value) : "",
      fechaEntradaRango: fechaRango ? normalizeText(fechaRango.value) : ""
    };
  }

  function buildEndpoint(handler) {
    var params = new URLSearchParams();
    params.set("handler", handler);

    var filters = getFilterValues();
    if (filters.empleadoFiltro) {
      params.set("empleadoFiltro", filters.empleadoFiltro);
    }
    if (filters.fechaEntradaInicio) {
      params.set("fechaEntradaInicio", filters.fechaEntradaInicio);
    }
    if (filters.fechaEntradaFin) {
      params.set("fechaEntradaFin", filters.fechaEntradaFin);
    }
    if (filters.fechaEntradaRango) {
      params.set("fechaEntradaRango", filters.fechaEntradaRango);
    }

    return window.location.pathname + "?" + params.toString();
  }

  async function fetchExportData(handler) {
    var response = null;
    try {
      response = await fetch(buildEndpoint(handler), {
        method: "GET",
        credentials: "same-origin",
        cache: "no-store",
        headers: {
          Accept: "application/json",
          "X-Requested-With": "XMLHttpRequest"
        }
      });
    } catch (error) {
      if (maybeRedirectForConnectivity(null, null, error)) {
        throw new Error(CONNECTIVITY_REDIRECT_MARKER);
      }
      throw error;
    }

    var payload = null;
    try {
      payload = await response.json();
    } catch (_err) {
      payload = null;
    }

    if (maybeRedirectForConnectivity(response, payload, null)) {
      throw new Error(CONNECTIVITY_REDIRECT_MARKER);
    }

    if (!response.ok) {
      throw new Error(payload && payload.mensaje ? payload.mensaje : "No se pudo preparar la exportacion.");
    }

    if (!payload || Number(payload.codigo) !== 0) {
      throw new Error(payload && payload.mensaje ? payload.mensaje : "Respuesta invalida para exportacion.");
    }

    return payload;
  }

  function toDateLabel(fechaI, fechaF) {
    var desde = normalizeText(fechaI);
    var hasta = normalizeText(fechaF);
    if (!desde && !hasta) {
      return "Rango: Sin filtro";
    }
    if (desde && hasta) {
      return "Rango: " + escapeHtml(desde) + " al " + escapeHtml(hasta);
    }
    return desde ? "Desde: " + escapeHtml(desde) : "Hasta: " + escapeHtml(hasta);
  }

  function buildWorkbook(title, metaLines, tableHtml) {
    var meta = (metaLines || [])
      .map(function (line) { return "<div class='meta-line'>" + line + "</div>"; })
      .join("");

    return "<html><head><meta charset='utf-8' /><style>" +
      "body{font-family:Calibri,Arial,sans-serif;margin:24px;color:#243026;}" +
      ".title{font-size:22px;font-weight:700;color:#184f42;margin:0 0 6px 0;}" +
      ".meta-line{font-size:12px;color:#4d6156;margin:0 0 2px 0;}" +
      "table{border-collapse:collapse;width:100%;font-size:12px;margin-top:12px;}" +
      "th,td{border:1px solid #cad8cd;padding:6px 8px;text-align:left;}" +
      "th{background:#dfeee6;color:#153f34;font-weight:700;}" +
      ".row-alt td{background:#f7fbf8;}" +
      ".num{text-align:right;}" +
      ".subtotal td{background:#fff3cf;font-weight:700;color:#6a4f0a;}" +
      ".total td{background:#d6ecdf;font-weight:700;color:#154132;}" +
      "</style></head><body>" +
      "<h1 class='title'>" + escapeHtml(title) + "</h1>" +
      meta + tableHtml +
      "</body></html>";
  }

  function downloadXls(fileName, html) {
    var blob = new Blob(["\ufeff", html], { type: "application/vnd.ms-excel;charset=utf-8;" });
    var url = URL.createObjectURL(blob);
    var link = document.createElement("a");
    link.href = url;
    link.download = (normalizeText(fileName) || "Reporte") + ".xls";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  function buildConsolidadoTable(rows) {
    var sorted = (rows || []).slice().sort(function (a, b) {
      return normalizeText(a && a.empleado).localeCompare(normalizeText(b && b.empleado), "es", {
        sensitivity: "base",
        numeric: true
      });
    });

    var totalHE = 0;
    var body = sorted.map(function (row, index) {
      totalHE += normalizeNumber(row && row.totalHE);
      var rowClass = index % 2 === 1 ? " class='row-alt'" : "";
      return "<tr" + rowClass + ">" +
        "<td>" + escapeHtml(row && row.empleado) + "</td>" +
        "<td class='num'>" + escapeHtml(formatNumber(row && row.totalHE, 2)) + "</td>" +
        "<td>" + escapeHtml(row && row.nombreEmpleado) + "</td>" +
        "<td>" + escapeHtml(row && row.ubicacion) + "</td>" +
        "<td>" + escapeHtml(row && row.nombreSupervisor) + "</td>" +
        "</tr>";
    }).join("");

    return "<table><thead><tr>" +
      "<th>Codigo</th><th>H. Extras</th><th>Nombre</th><th>Ubicacion</th><th>Supervisor</th>" +
      "</tr></thead><tbody>" +
      body +
      "<tr class='total'><td>TOTAL GENERAL</td><td class='num'>" + escapeHtml(formatNumber(totalHE, 2)) + "</td><td colspan='3'></td></tr>" +
      "</tbody></table>";
  }

  function parseDateKey(fechaText, entradaText) {
    var dateInput = normalizeText(fechaText);
    var timeInput = normalizeText(entradaText);
    var parsed = Date.parse(dateInput + " " + timeInput);
    if (Number.isFinite(parsed)) {
      return parsed;
    }

    parsed = Date.parse(dateInput);
    if (Number.isFinite(parsed)) {
      return parsed;
    }

    var slashDateMatch = dateInput.match(
      /^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:\s+(\d{1,2}):(\d{2})(?::(\d{2}))?)?$/
    );
    if (!slashDateMatch) {
      return Number.NEGATIVE_INFINITY;
    }

    var partA = Number.parseInt(slashDateMatch[1], 10);
    var partB = Number.parseInt(slashDateMatch[2], 10);
    var year = Number.parseInt(slashDateMatch[3], 10);
    var hours = Number.parseInt(slashDateMatch[4] || "0", 10);
    var minutes = Number.parseInt(slashDateMatch[5] || "0", 10);
    var seconds = Number.parseInt(slashDateMatch[6] || "0", 10);

    var day = partA;
    var month = partB;
    if (partA <= 12 && partB > 12) {
      day = partB;
      month = partA;
    }

    if (day < 1 || day > 31 || month < 1 || month > 12) {
      return Number.NEGATIVE_INFINITY;
    }

    return new Date(year, month - 1, day, hours, minutes, seconds).getTime();
  }

  function buildMarcadasTable(rows) {
    var sorted = (rows || []).slice().sort(function (a, b) {
      var empleadoCompare = normalizeText(a && a.empleado).localeCompare(normalizeText(b && b.empleado), "es", {
        sensitivity: "base",
        numeric: true
      });
      if (empleadoCompare !== 0) {
        return empleadoCompare;
      }
      return parseDateKey(a && a.fecha, a && a.entrada) - parseDateKey(b && b.fecha, b && b.entrada);
    });

    var currentEmpleado = "";
    var subtotalLaboradas = 0;
    var totalLaboradas = 0;
    var bodyRows = [];
    var zebra = 0;

    function pushSubtotal() {
      if (!currentEmpleado) {
        return;
      }
      bodyRows.push(
        "<tr class='subtotal'><td colspan='8'>Subtotal empleado " + escapeHtml(currentEmpleado) +
        "</td><td class='num'>" + escapeHtml(formatNumber(subtotalLaboradas, 2)) + "</td></tr>"
      );
    }

    sorted.forEach(function (row) {
      var empleado = normalizeText(row && row.empleado);
      if (empleado !== currentEmpleado) {
        pushSubtotal();
        currentEmpleado = empleado;
        subtotalLaboradas = 0;
      }

      var laboradas = normalizeNumber(row && row.laboradas);
      subtotalLaboradas += laboradas;
      totalLaboradas += laboradas;

      var rowClass = zebra % 2 === 1 ? " class='row-alt'" : "";
      zebra += 1;

      bodyRows.push("<tr" + rowClass + ">" +
        "<td>" + escapeHtml(row && row.empleado) + "</td>" +
        "<td>" + escapeHtml(row && row.nombres) + "</td>" +
        "<td>" + escapeHtml(row && row.apellidos) + "</td>" +
        "<td>" + escapeHtml(row && row.ubicadoEn) + "</td>" +
        "<td>" + escapeHtml(row && row.marcaEn) + "</td>" +
        "<td>" + escapeHtml(row && row.fecha) + "</td>" +
        "<td>" + escapeHtml(row && row.entrada) + "</td>" +
        "<td>" + escapeHtml(row && row.salida) + "</td>" +
        "<td class='num'>" + escapeHtml(formatNumber(laboradas, 2)) + "</td>" +
        "</tr>");
    });

    pushSubtotal();
    bodyRows.push(
      "<tr class='total'><td colspan='8'>TOTAL GENERAL LABORADAS</td><td class='num'>" +
      escapeHtml(formatNumber(totalLaboradas, 2)) + "</td></tr>"
    );

    return "<table><thead><tr>" +
      "<th>Empleado</th><th>Nombres</th><th>Apellidos</th><th>Ubicado en</th><th>Marca en</th>" +
      "<th>Fecha</th><th>Entrada</th><th>Salida</th><th>Laboradas</th>" +
      "</tr></thead><tbody>" + bodyRows.join("") + "</tbody></table>";
  }

  async function exportConsolidado() {
    toggleButtons(true);
    try {
      setStatus("1/3 Consultando consolidado...", "export-status-loading");
      var payload = await fetchExportData("ExportConsolidadoData");
      var rows = Array.isArray(payload.datos) ? payload.datos : [];
      if (rows.length === 0) {
        throw new Error("No hay datos para exportar en consolidado.");
      }

      setStatus("2/3 Aplicando formato del reporte...", "export-status-loading");
      var filtros = payload.filtros || {};
      var workbook = buildWorkbook("Consolidado de Horas Extras", [
        "Empleado: " + escapeHtml(filtros.idEmpleado || "Todos"),
        toDateLabel(filtros.fechaI, filtros.fechaF),
        "Generado: " + escapeHtml(new Date().toLocaleString("es-ES"))
      ], buildConsolidadoTable(rows));

      setStatus("3/3 Descargando archivo XLS...", "export-status-loading");
      downloadXls("ConsolidadoHE", workbook);
      setStatus("Exportacion de consolidado completada.", "export-status-ok", 6000);
    } catch (error) {
      if (error && error.message === CONNECTIVITY_REDIRECT_MARKER) {
        return;
      }

      setStatus(error && error.message ? error.message : "No se pudo exportar consolidado.", "export-status-error", 7000);
    } finally {
      toggleButtons(false);
    }
  }

  async function exportMarcadas() {
    toggleButtons(true);
    try {
      setStatus("1/3 Consultando reporte de marcadas...", "export-status-loading");
      var payload = await fetchExportData("ExportMarcadasData");
      var rows = Array.isArray(payload.datos) ? payload.datos : [];
      if (rows.length === 0) {
        throw new Error("No hay datos para exportar en reporte de marcadas.");
      }

      setStatus("2/3 Ordenando y calculando subtotales...", "export-status-loading");
      var filtros = payload.filtros || {};
      var workbook = buildWorkbook("Reporte de Marcadas", [
        "Empleado: " + escapeHtml(filtros.idEmpleado || "Todos"),
        toDateLabel(filtros.fechaI, filtros.fechaF),
        "Generado: " + escapeHtml(new Date().toLocaleString("es-ES"))
      ], buildMarcadasTable(rows));

      setStatus("3/3 Descargando archivo XLS...", "export-status-loading");
      downloadXls("MarcadasGeneralGH", workbook);
      setStatus("Exportacion de marcadas completada.", "export-status-ok", 6000);
    } catch (error) {
      if (error && error.message === CONNECTIVITY_REDIRECT_MARKER) {
        return;
      }

      setStatus(error && error.message ? error.message : "No se pudo exportar marcadas.", "export-status-error", 7000);
    } finally {
      toggleButtons(false);
    }
  }

  if (btnConsolidado) {
    btnConsolidado.addEventListener("click", function () {
      void exportConsolidado();
    });
  }

  if (btnMarcadas) {
    btnMarcadas.addEventListener("click", function () {
      void exportMarcadas();
    });
  }
})();
