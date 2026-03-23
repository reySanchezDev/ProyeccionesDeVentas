// Script global del frontend de horas extras.
(function () {
  var SAVE_DEBOUNCE_MS = 650;
  var BANNER_MESSAGE_DURATION_MS = 6000;
  var CONNECTIVITY_REDIRECT_MARKER = "__HE_REDIRECTING__";

  function normalizeHorasExtrasValue(rawValue) {
    var value = (rawValue || "").replace(",", ".");
    value = value.replace(/[^0-9.]/g, "");

    var parts = value.split(".");
    var integerPart = parts[0] || "";
    if (integerPart.length > 3) {
      integerPart = integerPart.slice(0, 3);
    }

    if (parts.length === 1) {
      return integerPart;
    }

    var decimalPart = parts.slice(1).join("");
    if (decimalPart.length > 2) {
      decimalPart = decimalPart.slice(0, 2);
    }

    return integerPart + "." + decimalPart;
  }

  function isValidHorasExtrasValue(value) {
    if (!value) {
      return false;
    }

    if (!/^\d{1,3}(\.\d{1,2})?$/.test(value)) {
      return false;
    }

    var numericValue = Number.parseFloat(value);
    return Number.isFinite(numericValue) && numericValue >= 0 && numericValue <= 999.99;
  }

  function formatHorasExtrasValue(value) {
    if (!isValidHorasExtrasValue(value)) {
      return value;
    }

    return Number.parseFloat(value).toFixed(2);
  }

  function shouldShowHorasFormatoError(value) {
    var normalized = normalizeHorasExtrasValue(value || "");
    return normalized.trim().length > 0 && !isValidHorasExtrasValue(normalized);
  }

  function toggleInputError(input, showError) {
    input.classList.toggle("input-invalid", showError);
  }

  function normalizeConnectivityType(rawType) {
    var normalized = String(rawType || "").trim().toLowerCase();
    if (normalized === "internet" || normalized === "offline" || normalized === "sin-internet") {
      return "internet";
    }
    if (normalized === "timeout") {
      return "timeout";
    }
    if (normalized === "conexion" || normalized === "servidor") {
      return "conexion";
    }
    return "error";
  }

  function buildDefaultConnectivityMessage(type) {
    if (type === "internet") {
      return "No hay conexion a internet en este dispositivo.";
    }
    if (type === "timeout") {
      return "El servidor excedio el tiempo de espera de respuesta.";
    }
    if (type === "conexion") {
      return "No se pudo conectar con el servidor de Horas Extras.";
    }
    return "Se detecto un error de conectividad.";
  }

  function normalizeConnectivityText(value, maxLength) {
    var text = String(value || "").trim();
    if (!text) {
      return "";
    }

    var limit = Number.isFinite(maxLength) && maxLength > 0 ? Math.floor(maxLength) : 500;
    return text.length <= limit ? text : text.slice(0, limit);
  }

  function isConnectivityMessage(message) {
    var text = String(message || "").toLowerCase();
    if (!text) {
      return false;
    }

    var keywords = [
      "conexion",
      "conectar",
      "no se pudo conectar",
      "timeout",
      "tiempo de espera",
      "network",
      "failed to fetch",
      "internet",
      "servicio no disponible",
      "503"
    ];

    return keywords.some(function (keyword) {
      return text.indexOf(keyword) >= 0;
    });
  }

  function buildConnectivityRedirectUrl(options) {
    var opts = options || {};
    var type = normalizeConnectivityType(opts.tipo);
    var currentPath = window.location.pathname || "/";
    var currentUrl = currentPath + (window.location.search || "");
    var params = new URLSearchParams();

    params.set("tipo", type);
    params.set("mensaje", normalizeConnectivityText(opts.mensaje || buildDefaultConnectivityMessage(type), 350));
    params.set("origen", normalizeConnectivityText(opts.origen || currentPath, 250));
    params.set("returnUrl", normalizeConnectivityText(opts.returnUrl || currentUrl, 450));
    params.set("fuente", normalizeConnectivityText(opts.fuente || "frontend", 120));

    if (opts.codigo) {
      params.set("codigo", String(opts.codigo));
    }

    if (opts.endpoint) {
      params.set("endpoint", normalizeConnectivityText(opts.endpoint, 280));
    }

    if (opts.detalleTecnico) {
      params.set("detalle", normalizeConnectivityText(opts.detalleTecnico, 900));
    }

    return "/ErrorConexion?" + params.toString();
  }

  function redirectToConnectivityError(options) {
    var currentPath = (window.location.pathname || "").toLowerCase();
    if (currentPath.indexOf("/errorconexion") === 0) {
      return false;
    }

    var targetUrl = buildConnectivityRedirectUrl(options || {});
    window.location.assign(targetUrl);
    return true;
  }

  function maybeRedirectForConnectivity(response, payload, error) {
    if (payload && payload.redirectUrl) {
      window.location.assign(payload.redirectUrl);
      return true;
    }

    var statusCode = response && Number(response.status || 0);
    var payloadType = normalizeConnectivityType(payload && payload.tipo);
    var payloadMessage = normalizeConnectivityText(payload && payload.mensaje, 350);
    var payloadDetail = normalizeConnectivityText(payload && payload.detalleTecnico, 900);
    var endpoint = normalizeConnectivityText(payload && payload.endpoint, 280);
    var errorMessage = normalizeConnectivityText(error && error.message, 350);
    var offline = typeof navigator !== "undefined" && navigator.onLine === false;

    if (offline) {
      return redirectToConnectivityError({
        tipo: "internet",
        mensaje: payloadMessage || "No hay conexion a internet en este dispositivo.",
        detalleTecnico: payloadDetail || "navigator.onLine=false",
        endpoint: endpoint,
        fuente: "browser-offline"
      });
    }

    if (statusCode === 503 || payloadType === "conexion" || payloadType === "timeout" || payloadType === "internet") {
      return redirectToConnectivityError({
        tipo: payloadType === "error" ? "conexion" : payloadType,
        mensaje: payloadMessage || errorMessage,
        detalleTecnico: payloadDetail || errorMessage,
        endpoint: endpoint,
        codigo: statusCode || 503,
        fuente: "fetch-response"
      });
    }

    if (error && error.name === "TypeError") {
      return redirectToConnectivityError({
        tipo: "conexion",
        mensaje: errorMessage || "Error de red del navegador al consultar el servicio.",
        detalleTecnico: errorMessage || "TypeError de red en fetch.",
        endpoint: endpoint,
        fuente: "fetch-exception"
      });
    }

    if (isConnectivityMessage(payloadMessage) || isConnectivityMessage(payloadDetail) || isConnectivityMessage(errorMessage)) {
      return redirectToConnectivityError({
        tipo: "conexion",
        mensaje: payloadMessage || errorMessage,
        detalleTecnico: payloadDetail || errorMessage,
        endpoint: endpoint,
        codigo: statusCode || undefined,
        fuente: "connectivity-keyword"
      });
    }

    return false;
  }

  function attachOfflineConnectivityWatcher() {
    if (typeof window === "undefined" || typeof window.addEventListener !== "function") {
      return;
    }

    function handleOfflineEvent() {
      if (typeof navigator === "undefined" || navigator.onLine !== false) {
        return;
      }

      redirectToConnectivityError({
        tipo: "internet",
        mensaje: "No hay conexion a internet en este dispositivo.",
        detalleTecnico: "El navegador reporto evento offline (navigator.onLine=false).",
        fuente: "browser-offline-event"
      });
    }

    window.addEventListener("offline", handleOfflineEvent);
    if (typeof navigator !== "undefined" && navigator.onLine === false) {
      handleOfflineEvent();
    }
  }

  window.HorasExtrasConnectivity = {
    redirectToConnectivityError: redirectToConnectivityError,
    maybeRedirectForConnectivity: maybeRedirectForConnectivity
  };

  function createBannerStatusSetter(feedbackNode) {
    var hideTimerId = 0;

    return function (message, cssClass) {
      if (!feedbackNode) {
        return;
      }

      if (hideTimerId) {
        window.clearTimeout(hideTimerId);
        hideTimerId = 0;
      }

      feedbackNode.classList.remove("save-banner-saving", "save-banner-ok", "save-banner-error");

      var text = (message || "").trim();
      if (!text) {
        feedbackNode.textContent = "";
        feedbackNode.hidden = true;
        return;
      }

      feedbackNode.textContent = text;
      feedbackNode.hidden = false;
      if (cssClass) {
        feedbackNode.classList.add(cssClass);
      }

      hideTimerId = window.setTimeout(function () {
        feedbackNode.textContent = "";
        feedbackNode.hidden = true;
        feedbackNode.classList.remove("save-banner-saving", "save-banner-ok", "save-banner-error");
        hideTimerId = 0;
      }, BANNER_MESSAGE_DURATION_MS);
    };
  }

  function attachSupervisorDateRangePicker() {
    var rangeInput = document.getElementById("fecha-rango-filtro");
    if (!rangeInput) {
      return;
    }

    var startInputId = rangeInput.getAttribute("data-start-id") || "FechaEntradaInicio";
    var endInputId = rangeInput.getAttribute("data-end-id") || "FechaEntradaFin";
    var startInput = document.getElementById(startInputId);
    var endInput = document.getElementById(endInputId);
    if (!startInput || !endInput) {
      return;
    }

    var form = document.getElementById("filtros-form");
    var empleadoInput = document.getElementById("EmpleadoFiltro");

    function resolveBuscarSubmitter() {
      if (!form) {
        return null;
      }

      var submitId = (form.getAttribute("data-filter-submit-id") || "").trim();
      if (!submitId) {
        return null;
      }

      var candidate = document.getElementById(submitId);
      if (!candidate || candidate.form !== form) {
        return null;
      }

      return candidate;
    }

    function submitBuscarForm() {
      if (!form) {
        return;
      }

      var submitter = resolveBuscarSubmitter();
      if (typeof form.requestSubmit === "function") {
        if (submitter) {
          form.requestSubmit(submitter);
        } else {
          form.requestSubmit();
        }
        return;
      }

      form.submit();
    }

    if (empleadoInput && form) {
      empleadoInput.addEventListener("keydown", function (event) {
        if (event.key !== "Enter") {
          return;
        }

        event.preventDefault();
        submitBuscarForm();
      });
    }

    if (
      typeof window.jQuery === "undefined" ||
      typeof window.moment === "undefined" ||
      !window.jQuery.fn ||
      !window.jQuery.fn.daterangepicker
    ) {
      rangeInput.readOnly = false;
      rangeInput.addEventListener("keydown", function (event) {
        if (event.key !== "Enter") {
          return;
        }

        event.preventDefault();
        submitBuscarForm();
      });

      rangeInput.addEventListener("blur", function () {
        var value = (rangeInput.value || "").trim();
        if (value.indexOf(" - ") > 0) {
          submitBuscarForm();
        }
      });
      return;
    }

    rangeInput.readOnly = false;

    var $ = window.jQuery;
    var $range = $(rangeInput);

    function parseIsoDate(value) {
      var raw = (value || "").trim();
      if (!raw) {
        return null;
      }

      var parsed = window.moment(raw, "YYYY-MM-DD", true);
      return parsed.isValid() ? parsed : null;
    }

    function parseRangeText(value) {
      var raw = (value || "").trim();
      if (!raw) {
        return null;
      }

      var startPart = "";
      var endPart = "";
      var spacedParts = raw.split(" - ");
      if (spacedParts.length === 2) {
        startPart = spacedParts[0].trim();
        endPart = spacedParts[1].trim();
      } else {
        var compactMatch = raw.match(
          /^((?:\d{4}-\d{1,2}-\d{1,2})|(?:\d{1,2}\/\d{1,2}\/\d{4}))\s*[-–—]\s*((?:\d{4}-\d{1,2}-\d{1,2})|(?:\d{1,2}\/\d{1,2}\/\d{4}))$/
        );
        if (!compactMatch) {
          return null;
        }

        startPart = (compactMatch[1] || "").trim();
        endPart = (compactMatch[2] || "").trim();
      }

      var start = window.moment(startPart, ["MM/DD/YYYY", "DD/MM/YYYY", "YYYY-MM-DD"], true);
      var end = window.moment(endPart, ["MM/DD/YYYY", "DD/MM/YYYY", "YYYY-MM-DD"], true);
      if (!start.isValid() || !end.isValid()) {
        return null;
      }

      return { start: start, end: end };
    }

    function normalizeRangeBounds(range) {
      if (!range || !range.start || !range.end) {
        return null;
      }

      var start = range.start.clone().startOf("day");
      var end = range.end.clone().startOf("day");
      if (end.isBefore(start, "day")) {
        var swap = start;
        start = end;
        end = swap;
      }

      return { start: start, end: end };
    }

    function toMoment(value) {
      if (!value) {
        return null;
      }

      if (typeof value.format === "function") {
        return value;
      }

      var parsed = window.moment(value);
      return parsed.isValid() ? parsed : null;
    }

    function setHidden(startMoment, endMoment) {
      var start = toMoment(startMoment);
      var end = toMoment(endMoment);
      startInput.value = start ? start.format("YYYY-MM-DD") : "";
      endInput.value = end ? end.format("YYYY-MM-DD") : "";
    }

    function setVisible(startMoment, endMoment) {
      var start = toMoment(startMoment);
      var end = toMoment(endMoment);
      if (!start || !end) {
        rangeInput.value = "";
        return;
      }

      rangeInput.value = start.format("MM/DD/YYYY") + " - " + end.format("MM/DD/YYYY");
    }

    function syncFromPicker(picker) {
      if (!picker || !picker.startDate || !picker.endDate) {
        setHidden(null, null);
        setVisible(null, null);
        return;
      }

      setHidden(picker.startDate, picker.endDate);
      setVisible(picker.startDate, picker.endDate);
    }

    function applyTypedRangeToPicker(submitAfterApply) {
      var normalizedRange = normalizeRangeBounds(parseRangeText(rangeInput.value || ""));
      if (!normalizedRange) {
        return false;
      }

      var picker = $range.data("daterangepicker");
      if (picker) {
        picker.setStartDate(normalizedRange.start.clone());
        picker.setEndDate(normalizedRange.end.clone());
        picker.updateView();
      }

      setHidden(normalizedRange.start, normalizedRange.end);
      setVisible(normalizedRange.start, normalizedRange.end);

      if (submitAfterApply) {
        submitBuscarForm();
      }

      return true;
    }

    function setPickerActiveSide(picker, side) {
      if (!picker || !picker.container) {
        return;
      }

      var $startField = picker.container.find('input[name="daterangepicker_start"]');
      var $endField = picker.container.find('input[name="daterangepicker_end"]');
      if (!$startField.length || !$endField.length) {
        return;
      }

      var activateEnd = side === "end";
      $startField.toggleClass("active", !activateEnd);
      $endField.toggleClass("active", activateEnd);
    }

    function focusPickerSide(picker, side) {
      if (!picker || !picker.container) {
        return;
      }

      var $target = picker.container.find(
        side === "end"
          ? 'input[name="daterangepicker_end"]'
          : 'input[name="daterangepicker_start"]'
      );
      if (!$target.length) {
        return;
      }

      window.setTimeout(function () {
        setPickerActiveSide(picker, side);
        $target.trigger("focus");
        $target.select();
      }, 0);
    }

    function wireDynamicPickerFocus(picker) {
      if (!picker || !picker.container || picker.container.data("dynamic-focus-wired")) {
        return;
      }

      var $container = picker.container;
      $container.data("dynamic-focus-wired", "1");
      var activeSide = "start";

      $container.on("focusin.dynamicfocus", 'input[name="daterangepicker_start"]', function () {
        activeSide = "start";
        setPickerActiveSide(picker, "start");
      });

      $container.on("focusin.dynamicfocus", 'input[name="daterangepicker_end"]', function () {
        activeSide = "end";
        setPickerActiveSide(picker, "end");
      });

      $container.on("keydown.dynamicfocus", 'input[name="daterangepicker_start"], input[name="daterangepicker_end"]', function (event) {
        if (event.key !== "Tab") {
          return;
        }

        event.preventDefault();
        var currentSide = event.target && event.target.name === "daterangepicker_end" ? "end" : "start";
        activeSide = currentSide === "start" ? "end" : "start";
        focusPickerSide(picker, activeSide);
      });

      $container.on("mouseup.dynamicfocus", "td.available", function () {
        window.setTimeout(function () {
          if (!picker.isShowing) {
            return;
          }

          // Al elegir fecha inicial, mueve foco inmediato a fecha final.
          if (activeSide === "start") {
            activeSide = "end";
            focusPickerSide(picker, "end");
          }
        }, 0);
      });
    }

    var localeConfig = {
      format: "MM/DD/YYYY",
      separator: " - ",
      applyLabel: "Aplicar",
      cancelLabel: "Limpiar",
      fromLabel: "Desde",
      toLabel: "Hasta",
      customRangeLabel: "Personalizado",
      weekLabel: "S",
      daysOfWeek: ["Do", "Lu", "Ma", "Mi", "Ju", "Vi", "Sa"],
      monthNames: [
        "Enero",
        "Febrero",
        "Marzo",
        "Abril",
        "Mayo",
        "Junio",
        "Julio",
        "Agosto",
        "Septiembre",
        "Octubre",
        "Noviembre",
        "Diciembre"
      ],
      firstDay: 1
    };

    var startDate = parseIsoDate(startInput.value);
    var endDate = parseIsoDate(endInput.value);
    if (!startDate || !endDate) {
      var parsedRange = parseRangeText(rangeInput.value || "");
      if (parsedRange) {
        startDate = parsedRange.start;
        endDate = parsedRange.end;
      }
    }
    var defaultDate = window.moment().startOf("day");

    if (startDate && !endDate) {
      endDate = startDate.clone();
    } else if (!startDate && endDate) {
      startDate = endDate.clone();
    }

    $range.daterangepicker(
      {
        autoUpdateInput: false,
        autoApply: false,
        linkedCalendars: true,
        showDropdowns: true,
        opens: "center",
        locale: localeConfig,
        startDate: startDate || defaultDate,
        endDate: endDate || startDate || defaultDate
      },
      function (start, end) {
        setHidden(start, end);
        setVisible(start, end);
      }
    );

    if (startDate && endDate) {
      setHidden(startDate, endDate);
      setVisible(startDate, endDate);
    } else {
      setHidden(null, null);
      setVisible(null, null);
    }

    $range.on("apply.daterangepicker", function (_event, picker) {
      syncFromPicker(picker);
      submitBuscarForm();
    });

    $range.on("show.daterangepicker", function (_event, picker) {
      wireDynamicPickerFocus(picker);

      // Al abrir, inicio es el foco por defecto.
      focusPickerSide(picker, "start");
    });

    $range.on("cancel.daterangepicker", function () {
      setHidden(null, null);
      setVisible(null, null);
      submitBuscarForm();
    });

    if (form) {
      form.addEventListener("submit", function () {
        if (!(rangeInput.value || "").trim()) {
          setHidden(null, null);
          return;
        }

        if (applyTypedRangeToPicker(false)) {
          return;
        }

        var picker = $range.data("daterangepicker");
        if (!picker) {
          var parsed = parseRangeText(rangeInput.value || "");
          if (parsed) {
            var normalizedRange = normalizeRangeBounds(parsed);
            if (normalizedRange) {
              setHidden(normalizedRange.start, normalizedRange.end);
              setVisible(normalizedRange.start, normalizedRange.end);
            } else {
              setHidden(null, null);
            }
          } else {
            setHidden(null, null);
          }
          return;
        }

        syncFromPicker(picker);
      });
    }

    rangeInput.addEventListener("keydown", function (event) {
      if (event.key !== "Enter") {
        return;
      }

      event.preventDefault();
      if (!applyTypedRangeToPicker(true)) {
        submitBuscarForm();
      }
    });

    rangeInput.addEventListener("change", function () {
      applyTypedRangeToPicker(false);
    });
  }

  function getRowPayload(row) {
    var idTabla = Number.parseInt(row.getAttribute("data-registro-id") || "", 10);
    var idSqlite = (row.getAttribute("data-id-sqlite") || "").trim();
    var sucursal = (row.getAttribute("data-sucursal") || "").trim();
    var empleado = (row.getAttribute("data-empleado") || "").trim();
    var fechaEntro = (row.getAttribute("data-fecha-entro") || "").trim();
    var horasInput = row.querySelector(".hext-input");
    var conceptoInput = row.querySelector(".concept-input");

    var hasIdTabla = Number.isInteger(idTabla) && idTabla > 0;
    var hasLegacyFallback = idSqlite && sucursal && empleado && fechaEntro;

    if ((!hasIdTabla && !hasLegacyFallback) || !horasInput || !conceptoInput) {
      return null;
    }

    var horasRaw = normalizeHorasExtrasValue(horasInput.value || "");
    var concepto = (conceptoInput.value || "").trim();

    return {
      idTabla: hasIdTabla ? idTabla : null,
      hExt: horasRaw,
      descripcion: concepto,
      idSqlite: idSqlite,
      sucursal: sucursal,
      empleado: empleado,
      fechaEntro: fechaEntro
    };
  }

  function getPayloadSignature(payload) {
    return [
      payload.idTabla,
      payload.hExt,
      payload.descripcion,
      payload.idSqlite,
      payload.sucursal,
      payload.empleado,
      payload.fechaEntro
    ].join("|");
  }

  function attachSupervisorAutoSave() {
    var supervisorShell = document.querySelector(".supervisor-shell[data-can-edit]");
    if (supervisorShell && supervisorShell.getAttribute("data-can-edit") === "false") {
      return;
    }

    var rows = document.querySelectorAll("tr[data-registro-id]");
    if (!rows.length) {
      return;
    }

    var feedbackNode = document.getElementById("supervisor-save-feedback");
    var setFeedback = createBannerStatusSetter(feedbackNode);
    var antiforgeryTokenInput = document.querySelector("#filtros-form input[name='__RequestVerificationToken']");
    var antiforgeryToken = antiforgeryTokenInput ? antiforgeryTokenInput.value : "";
    var endpoint = window.location.pathname + "?handler=AprobarHoraExtra";
    var rowStates = new WeakMap();

    function getState(row) {
      var state = rowStates.get(row);
      if (state) {
        return state;
      }

      var payload = getRowPayload(row);
      var initialSignature = payload ? getPayloadSignature(payload) : "";

      state = {
        timerId: 0,
        requestId: 0,
        lastSavedSignature: initialSignature,
        dirty: false
      };

      rowStates.set(row, state);
      return state;
    }

    function validateRowPayload(row) {
      var payload = getRowPayload(row);
      if (!payload) {
        return { valid: false, message: "Registro invalido." };
      }

      var horasInput = row.querySelector(".hext-input");
      if (!horasInput) {
        return { valid: false, message: "Campo de horas no encontrado." };
      }

      var validHoras = isValidHorasExtrasValue(payload.hExt);
      toggleInputError(horasInput, shouldShowHorasFormatoError(horasInput.value || ""));
      if (!validHoras) {
        var hasContent = (horasInput.value || "").trim().length > 0;
        return {
          valid: false,
          message: hasContent
            ? "Formato invalido en Extras (use hasta 2 decimales)."
            : "El campo Extras es requerido."
        };
      }

      if (!payload.descripcion) {
        return { valid: false, message: "El campo Concepto es requerido." };
      }

      if (payload.descripcion.length > 500) {
        return { valid: false, message: "El concepto excede 500 caracteres." };
      }

      payload.hExt = formatHorasExtrasValue(payload.hExt);
      return { valid: true, payload: payload };
    }

    async function saveRow(row) {
      var validation = validateRowPayload(row);
      if (!validation.valid) {
        setFeedback(validation.message, "save-banner-error");
        return;
      }

      var payload = validation.payload;
      var state = getState(row);
      var signature = getPayloadSignature(payload);

      if (signature === state.lastSavedSignature) {
        state.dirty = false;
        return;
      }

      state.requestId += 1;
      var currentRequestId = state.requestId;

      setFeedback("Guardando cambios...", "save-banner-saving");

      try {
        var response = await fetch(endpoint, {
          method: "POST",
          credentials: "same-origin",
          headers: Object.assign(
            { "Content-Type": "application/json" },
            antiforgeryToken ? { RequestVerificationToken: antiforgeryToken } : {}
          ),
          body: JSON.stringify(payload)
        });

        var responsePayload = null;
        try {
          responsePayload = await response.json();
        } catch (_err) {
          responsePayload = null;
        }

        if (currentRequestId !== getState(row).requestId) {
          return;
        }

        if (maybeRedirectForConnectivity(response, responsePayload, null)) {
          return;
        }

        if (response.ok && responsePayload && Number(responsePayload.codigo) === 0) {
          state.lastSavedSignature = signature;
          state.dirty = false;
          setFeedback("Guardado", "save-banner-ok");
          return;
        }

        var errorMessage = responsePayload && responsePayload.mensaje
          ? responsePayload.mensaje
          : "No se pudo guardar el cambio.";

        setFeedback(errorMessage, "save-banner-error");
      } catch (error) {
        if (currentRequestId === getState(row).requestId) {
          if (maybeRedirectForConnectivity(null, null, error)) {
            return;
          }
          setFeedback("Error de red al guardar.", "save-banner-error");
        }
      }
    }

    function scheduleSave(row, delayMs) {
      var state = getState(row);
      if (state.timerId) {
        window.clearTimeout(state.timerId);
      }

      var payload = getRowPayload(row);
      if (!payload) {
        state.dirty = false;
        return;
      }

      var signature = getPayloadSignature(payload);
      if (signature === state.lastSavedSignature) {
        state.dirty = false;
        return;
      }

      state.dirty = true;
      setFeedback("Pendiente de guardar...", "save-banner-saving");
      state.timerId = window.setTimeout(function () {
        state.timerId = 0;
        void saveRow(row);
      }, delayMs);
    }

    rows.forEach(function (row) {
      var horasInput = row.querySelector(".hext-input");
      var conceptoInput = row.querySelector(".concept-input");
      if (!horasInput || !conceptoInput) {
        return;
      }

      var initialNormalized = normalizeHorasExtrasValue(horasInput.value || "");
      if (isValidHorasExtrasValue(initialNormalized)) {
        horasInput.value = formatHorasExtrasValue(initialNormalized);
      }
      toggleInputError(horasInput, shouldShowHorasFormatoError(horasInput.value || ""));

      horasInput.addEventListener("input", function (event) {
        var normalized = normalizeHorasExtrasValue(event.target.value || "");
        if (event.target.value !== normalized) {
          event.target.value = normalized;
        }

        toggleInputError(event.target, shouldShowHorasFormatoError(event.target.value || ""));
        scheduleSave(row, SAVE_DEBOUNCE_MS);
      });

      horasInput.addEventListener("blur", function (event) {
        var normalized = normalizeHorasExtrasValue(event.target.value || "");
        if (isValidHorasExtrasValue(normalized)) {
          event.target.value = formatHorasExtrasValue(normalized);
          toggleInputError(event.target, false);
          scheduleSave(row, 0);
          return;
        }

        event.target.value = normalized;
        toggleInputError(event.target, shouldShowHorasFormatoError(event.target.value || ""));
        var hasContent = normalized.trim().length > 0;
        setFeedback(
          hasContent
            ? "Formato invalido en Extras (use hasta 2 decimales)."
            : "El campo Extras es requerido.",
          "save-banner-error"
        );
      });

      conceptoInput.addEventListener("input", function () {
        scheduleSave(row, SAVE_DEBOUNCE_MS);
      });

      conceptoInput.addEventListener("blur", function () {
        scheduleSave(row, 0);
      });
    });
  }

  function attachSupervisorTableSorting() {
    var table = document.querySelector(".supervisor-table-scroll .table");
    if (!table || !table.tBodies || table.tBodies.length === 0) {
      return;
    }

    var tbody = table.tBodies[0];
    var rows = Array.prototype.slice.call(tbody.querySelectorAll("tr[data-registro-id]"));
    if (!rows.length) {
      return;
    }

    var triggers = Array.prototype.slice.call(table.querySelectorAll(".table-sort-trigger"));
    if (!triggers.length) {
      return;
    }

    rows.forEach(function (row, index) {
      row.setAttribute("data-sort-base-index", String(index));
    });

    function normalizeNumber(rawValue) {
      var normalized = (rawValue || "").trim().replace(",", ".");
      if (!normalized) {
        return Number.NEGATIVE_INFINITY;
      }

      var parsed = Number.parseFloat(normalized);
      return Number.isFinite(parsed) ? parsed : Number.NEGATIVE_INFINITY;
    }

    function normalizeDate(rawValue) {
      var normalized = (rawValue || "").trim();
      if (!normalized) {
        return Number.NEGATIVE_INFINITY;
      }

      var direct = Date.parse(normalized);
      if (Number.isFinite(direct)) {
        return direct;
      }

      var slashDateMatch = normalized.match(
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

    function readCellValue(row, colIndex, sortType) {
      var cell = row.cells[colIndex];
      if (!cell) {
        return sortType === "text" || sortType === "input-text" ? "" : Number.NEGATIVE_INFINITY;
      }

      if (sortType === "number-input") {
        var numericInput = cell.querySelector("input");
        return normalizeNumber(numericInput ? numericInput.value : cell.textContent);
      }

      if (sortType === "input-text") {
        var textInput = cell.querySelector("input");
        return ((textInput ? textInput.value : cell.textContent) || "").trim().toLocaleLowerCase();
      }

      if (sortType === "number") {
        return normalizeNumber(cell.textContent);
      }

      if (sortType === "date") {
        return normalizeDate(cell.textContent);
      }

      return (cell.textContent || "").trim().toLocaleLowerCase();
    }

    function clearSortState() {
      triggers.forEach(function (trigger) {
        trigger.removeAttribute("data-sort-state");
        var th = trigger.closest("th");
        if (th) {
          th.setAttribute("aria-sort", "none");
        }
      });
    }

    function sortByTrigger(trigger) {
      var colIndex = Number.parseInt(trigger.getAttribute("data-sort-col") || "", 10);
      if (!Number.isInteger(colIndex)) {
        return;
      }

      var sortType = (trigger.getAttribute("data-sort-type") || "text").trim();
      var nextDirection = trigger.getAttribute("data-sort-state") === "asc" ? "desc" : "asc";
      var directionFactor = nextDirection === "asc" ? 1 : -1;

      clearSortState();
      trigger.setAttribute("data-sort-state", nextDirection);

      var th = trigger.closest("th");
      if (th) {
        th.setAttribute("aria-sort", nextDirection === "asc" ? "ascending" : "descending");
      }

      var orderedRows = rows.slice().sort(function (rowA, rowB) {
        var valueA = readCellValue(rowA, colIndex, sortType);
        var valueB = readCellValue(rowB, colIndex, sortType);

        var comparison = 0;
        if (typeof valueA === "number" && typeof valueB === "number") {
          comparison = valueA - valueB;
        } else {
          comparison = String(valueA).localeCompare(String(valueB), "es", {
            sensitivity: "base",
            numeric: true
          });
        }

        if (comparison === 0) {
          var baseA = Number.parseInt(rowA.getAttribute("data-sort-base-index") || "0", 10);
          var baseB = Number.parseInt(rowB.getAttribute("data-sort-base-index") || "0", 10);
          comparison = baseA - baseB;
        }

        return comparison * directionFactor;
      });

      orderedRows.forEach(function (row) {
        tbody.appendChild(row);
      });
    }

    triggers.forEach(function (trigger) {
      trigger.addEventListener("click", function () {
        sortByTrigger(trigger);
      });
    });
  }

  function attachSupervisorExports() {
    var btnMarcadas = document.getElementById("btn-export-marcadas");
    var btnConsolidado = document.getElementById("btn-export-consolidado");
    var btnConsolidadoPivot = document.getElementById("btn-export-consolidado-pivot");
    if (!btnMarcadas && !btnConsolidado && !btnConsolidadoPivot) {
      return;
    }

    var feedbackNode = document.getElementById("supervisor-export-feedback");
    var statusHideTimerId = 0;

    function setStatus(message, cssClass, autoHideMs) {
      if (!feedbackNode) {
        return;
      }

      if (statusHideTimerId) {
        window.clearTimeout(statusHideTimerId);
        statusHideTimerId = 0;
      }

      feedbackNode.classList.remove("export-status-loading", "export-status-ok", "export-status-error");

      var text = (message || "").trim();
      if (!text) {
        feedbackNode.hidden = true;
        feedbackNode.textContent = "";
        return;
      }

      feedbackNode.hidden = false;
      feedbackNode.textContent = text;
      if (cssClass) {
        feedbackNode.classList.add(cssClass);
      }

      var hideDelay = Number.parseInt(autoHideMs, 10);
      if (Number.isFinite(hideDelay) && hideDelay > 0) {
        statusHideTimerId = window.setTimeout(function () {
          feedbackNode.hidden = true;
          feedbackNode.textContent = "";
          feedbackNode.classList.remove("export-status-loading", "export-status-ok", "export-status-error");
          statusHideTimerId = 0;
        }, hideDelay);
      }
    }

    function disableExportButtons(disabled) {
      if (btnMarcadas) {
        btnMarcadas.disabled = !!disabled;
      }
      if (btnConsolidado) {
        btnConsolidado.disabled = !!disabled;
      }
      if (btnConsolidadoPivot) {
        btnConsolidadoPivot.disabled = !!disabled;
      }
    }

    function escapeHtml(value) {
      return String(value || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
    }

    function normalizeText(value) {
      return String(value || "").trim();
    }

    function normalizeNumber(value) {
      if (value === null || value === undefined || value === "") {
        return 0;
      }

      var numeric = Number.parseFloat(String(value).replace(",", "."));
      return Number.isFinite(numeric) ? numeric : 0;
    }

    function formatNumber(value, decimals) {
      var numeric = normalizeNumber(value);
      var digits = Number.isInteger(decimals) && decimals >= 0 ? decimals : 2;
      return numeric.toFixed(digits);
    }

    function formatAsMonthDayYear(dateValue) {
      if (!(dateValue instanceof Date) || Number.isNaN(dateValue.getTime())) {
        return "";
      }

      var month = String(dateValue.getMonth() + 1).padStart(2, "0");
      var day = String(dateValue.getDate()).padStart(2, "0");
      var year = String(dateValue.getFullYear());
      return month + "/" + day + "/" + year;
    }

    function toDateOnly(dateValue) {
      if (!(dateValue instanceof Date) || Number.isNaN(dateValue.getTime())) {
        return null;
      }

      return new Date(dateValue.getFullYear(), dateValue.getMonth(), dateValue.getDate());
    }

    function sameDay(left, right) {
      var dayLeft = toDateOnly(left);
      var dayRight = toDateOnly(right);
      return !!dayLeft && !!dayRight && dayLeft.getTime() === dayRight.getTime();
    }

    function buildCurrentMonthRange() {
      var now = new Date();
      return {
        start: new Date(now.getFullYear(), now.getMonth(), 1),
        end: new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59, 999)
      };
    }

    function isLegacyDefaultFifteenDayRange(range) {
      if (!range || !range.start || !range.end) {
        return false;
      }

      var today = toDateOnly(new Date());
      var fifteenDaysAgo = today ? new Date(today.getFullYear(), today.getMonth(), today.getDate() - 15) : null;
      if (!today || !fifteenDaysAgo) {
        return false;
      }

      return sameDay(range.start, fifteenDaysAgo) && sameDay(range.end, today);
    }

    function parseFlexibleDate(value) {
      var text = normalizeText(value);
      if (!text) {
        return null;
      }

      var timestamp = Date.parse(text);
      if (Number.isFinite(timestamp)) {
        return new Date(timestamp);
      }

      var isoMatch = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/);
      if (isoMatch) {
        var isoYear = Number.parseInt(isoMatch[1], 10);
        var isoMonth = Number.parseInt(isoMatch[2], 10);
        var isoDay = Number.parseInt(isoMatch[3], 10);
        if (isoMonth >= 1 && isoMonth <= 12 && isoDay >= 1 && isoDay <= 31) {
          return new Date(isoYear, isoMonth - 1, isoDay);
        }
      }

      var slashMatch = text.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
      if (!slashMatch) {
        return null;
      }

      var partA = Number.parseInt(slashMatch[1], 10);
      var partB = Number.parseInt(slashMatch[2], 10);
      var year = Number.parseInt(slashMatch[3], 10);

      var month = partA;
      var day = partB;
      if (partA > 12 && partB <= 12) {
        day = partA;
        month = partB;
      }

      if (month < 1 || month > 12 || day < 1 || day > 31) {
        return null;
      }

      return new Date(year, month - 1, day);
    }

    function parseRangeText(value) {
      var text = normalizeText(value);
      if (!text) {
        return null;
      }

      var startPart = "";
      var endPart = "";
      var spaced = text.split(" - ");
      if (spaced.length === 2) {
        startPart = normalizeText(spaced[0]);
        endPart = normalizeText(spaced[1]);
      } else {
        var compact = text.match(
          /^((?:\d{4}-\d{1,2}-\d{1,2})|(?:\d{1,2}\/\d{1,2}\/\d{4}))\s*[-–—]\s*((?:\d{4}-\d{1,2}-\d{1,2})|(?:\d{1,2}\/\d{1,2}\/\d{4}))$/
        );
        if (!compact) {
          return null;
        }

        startPart = normalizeText(compact[1]);
        endPart = normalizeText(compact[2]);
      }

      var startDate = parseFlexibleDate(startPart);
      var endDate = parseFlexibleDate(endPart);
      if (!startDate || !endDate) {
        return null;
      }

      return { start: startDate, end: endDate };
    }

    function normalizeDateRangeBounds(startDate, endDate) {
      var start = startDate ? new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate()) : null;
      var end = endDate ? new Date(endDate.getFullYear(), endDate.getMonth(), endDate.getDate(), 23, 59, 59, 999) : null;
      if (!start || !end) {
        return null;
      }

      if (end.getTime() < start.getTime()) {
        var swapStart = new Date(end.getFullYear(), end.getMonth(), end.getDate());
        var swapEnd = new Date(start.getFullYear(), start.getMonth(), start.getDate(), 23, 59, 59, 999);
        return { start: swapStart, end: swapEnd };
      }

      return { start: start, end: end };
    }

    function resolveEffectiveDateRange() {
      var fechaInicioInput = document.getElementById("FechaEntradaInicio");
      var fechaFinInput = document.getElementById("FechaEntradaFin");
      var fechaRangoInput = document.getElementById("FechaEntradaRango") || document.getElementById("fecha-rango-filtro");
      var rangeText = normalizeText(fechaRangoInput ? fechaRangoInput.value : "");

      var parsedRangeText = parseRangeText(rangeText);
      var startDate = parsedRangeText ? parsedRangeText.start : parseFlexibleDate(fechaInicioInput ? fechaInicioInput.value : "");
      var endDate = parsedRangeText ? parsedRangeText.end : parseFlexibleDate(fechaFinInput ? fechaFinInput.value : "");

      if (!startDate && !endDate) {
        return buildCurrentMonthRange();
      }

      if (startDate && !endDate) {
        endDate = new Date(startDate.getTime());
      } else if (!startDate && endDate) {
        startDate = new Date(endDate.getTime());
      }

      if (!startDate || !endDate) {
        return buildCurrentMonthRange();
      }

      var normalizedRange = normalizeDateRangeBounds(startDate, endDate);
      if (!normalizedRange) {
        return buildCurrentMonthRange();
      }

      // El rango por defecto del listado es ultimos 15 dias.
      // Para el consolidado pivotado, si sigue en ese valor por defecto,
      // se toma como "sin filtro" y se pivota solo el mes actual.
      if (isLegacyDefaultFifteenDayRange(normalizedRange)) {
        return buildCurrentMonthRange();
      }

      return normalizedRange;
    }

    function buildMonthBuckets(startDate, endDate) {
      var monthNames = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];
      var startMonth = new Date(startDate.getFullYear(), startDate.getMonth(), 1);
      var endMonth = new Date(endDate.getFullYear(), endDate.getMonth(), 1);
      var includeYear = startDate.getFullYear() !== endDate.getFullYear();
      var buckets = [];
      var cursor = new Date(startMonth.getTime());

      while (cursor.getTime() <= endMonth.getTime()) {
        var key = String(cursor.getFullYear()) + "-" + String(cursor.getMonth() + 1).padStart(2, "0");
        var label = monthNames[cursor.getMonth()] + (includeYear ? " " + String(cursor.getFullYear()) : "");
        buckets.push({ key: key, label: label });
        cursor = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1);
      }

      return buckets;
    }

    function parseDateKey(fechaText, entradaText) {
      var dateInput = normalizeText(fechaText);
      var timeInput = normalizeText(entradaText);
      var composite = dateInput + " " + timeInput;

      var parsed = Date.parse(composite);
      if (Number.isFinite(parsed)) {
        return parsed;
      }

      parsed = Date.parse(dateInput);
      if (Number.isFinite(parsed)) {
        return parsed;
      }

      var slash = dateInput.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
      if (!slash) {
        return Number.NEGATIVE_INFINITY;
      }

      var partA = Number.parseInt(slash[1], 10);
      var partB = Number.parseInt(slash[2], 10);
      var year = Number.parseInt(slash[3], 10);

      var day = partA;
      var month = partB;
      if (partA <= 12 && partB > 12) {
        day = partB;
        month = partA;
      }

      if (day < 1 || day > 31 || month < 1 || month > 12) {
        return Number.NEGATIVE_INFINITY;
      }

      return new Date(year, month - 1, day).getTime();
    }

    function readLoadedTableRows() {
      var table = document.querySelector(".supervisor-table-scroll table");
      if (!table || !table.tBodies || table.tBodies.length === 0) {
        return [];
      }

      var rows = Array.prototype.slice.call(table.tBodies[0].querySelectorAll("tr[data-empleado]"));
      return rows.map(function (row) {
        var cells = row.cells || [];
        var fechaEntroRaw = normalizeText(row.getAttribute("data-fecha-entro"));
        var entradaText = normalizeText(cells[5] ? cells[5].textContent : "");
        var timestamp = parseDateKey(entradaText, "");
        if (!Number.isFinite(timestamp)) {
          var fechaEntroDate = parseFlexibleDate(fechaEntroRaw);
          timestamp = fechaEntroDate ? fechaEntroDate.getTime() : Number.NEGATIVE_INFINITY;
        }
        var extrasInput = row.querySelector(".hext-input");
        var extrasValue = extrasInput ? extrasInput.value : (cells[8] ? cells[8].textContent : "");

        return {
          empleado: normalizeText(row.getAttribute("data-empleado")) || normalizeText(cells[2] ? cells[2].textContent : ""),
          nombre: normalizeText(cells[3] ? cells[3].textContent : ""),
          sucursal: normalizeText(row.getAttribute("data-sucursal")) || normalizeText(cells[1] ? cells[1].textContent : ""),
          entrada: entradaText,
          fechaKey: timestamp,
          hExtras: normalizeNumber(extrasValue)
        };
      }).filter(function (row) {
        return row.empleado.length > 0 && Number.isFinite(row.fechaKey);
      });
    }

    function monthKeyFromTimestamp(timestamp) {
      if (!Number.isFinite(timestamp)) {
        return "";
      }

      var date = new Date(timestamp);
      return String(date.getFullYear()) + "-" + String(date.getMonth() + 1).padStart(2, "0");
    }

    function buildPivotConsolidadoTable(rows, monthBuckets, range) {
      var monthSet = new Set((monthBuckets || []).map(function (bucket) { return bucket.key; }));
      var employeeMap = new Map();
      var monthTotals = {};

      (monthBuckets || []).forEach(function (bucket) {
        monthTotals[bucket.key] = 0;
      });

      (rows || []).forEach(function (row) {
        if (!row || !Number.isFinite(row.fechaKey)) {
          return;
        }

        if (row.fechaKey < range.start.getTime() || row.fechaKey > range.end.getTime()) {
          return;
        }

        var monthKey = monthKeyFromTimestamp(row.fechaKey);
        if (!monthSet.has(monthKey)) {
          return;
        }

        var employeeKey = normalizeText(row.empleado) + "|" + normalizeText(row.sucursal);
        if (!employeeMap.has(employeeKey)) {
          employeeMap.set(employeeKey, {
            empleado: normalizeText(row.empleado),
            nombre: normalizeText(row.nombre),
            sucursal: normalizeText(row.sucursal),
            monthHours: {},
            total: 0
          });
        }

        var employee = employeeMap.get(employeeKey);
        var hours = normalizeNumber(row.hExtras);
        employee.monthHours[monthKey] = normalizeNumber(employee.monthHours[monthKey]) + hours;
        employee.total += hours;
        monthTotals[monthKey] = normalizeNumber(monthTotals[monthKey]) + hours;
      });

      var orderedEmployees = Array.from(employeeMap.values()).sort(function (left, right) {
        return normalizeText(left.empleado).localeCompare(normalizeText(right.empleado), "es", {
          sensitivity: "base",
          numeric: true
        });
      });
      var employeesWithHours = orderedEmployees.filter(function (employee) {
        return normalizeNumber(employee.total) > 0;
      });

      if (!employeesWithHours.length) {
        return "";
      }

      var headerMonths = monthBuckets.map(function (bucket) {
        return "<th class='num'>" + escapeHtml(bucket.label) + "</th>";
      }).join("");

      var bodyRows = employeesWithHours.map(function (employee, index) {
        var monthCells = monthBuckets.map(function (bucket) {
          var value = normalizeNumber(employee.monthHours[bucket.key]);
          return "<td class='num'>" + escapeHtml(formatNumber(value, 2)) + "</td>";
        }).join("");

        var rowClass = index % 2 === 1 ? " class='row-alt'" : "";
        return "<tr" + rowClass + ">" +
          "<td>" + escapeHtml(employee.sucursal) + "</td>" +
          "<td>" + escapeHtml(employee.empleado) + "</td>" +
          "<td>" + escapeHtml(employee.nombre) + "</td>" +
          monthCells +
          "<td class='num'>" + escapeHtml(formatNumber(employee.total, 2)) + "</td>" +
          "</tr>";
      }).join("");

      var totalByMonthCells = monthBuckets.map(function (bucket) {
        return "<td class='num'>" + escapeHtml(formatNumber(monthTotals[bucket.key], 2)) + "</td>";
      }).join("");

      var totalGeneral = employeesWithHours.reduce(function (acc, row) {
        return acc + normalizeNumber(row.total);
      }, 0);

      var totalRow = "<tr class='total'>" +
        "<td colspan='3'>TOTAL GENERAL</td>" +
        totalByMonthCells +
        "<td class='num'>" + escapeHtml(formatNumber(totalGeneral, 2)) + "</td>" +
        "</tr>";

      return "<table><thead>" +
        "<tr><th rowspan='2'>Sucursal</th><th rowspan='2'>Codigo</th><th rowspan='2'>Nombre</th>" +
        "<th colspan='" + String(monthBuckets.length) + "' class='center'>Mes</th><th rowspan='2'>Total H. Extras</th></tr>" +
        "<tr>" + headerMonths + "</tr>" +
        "</thead><tbody>" + bodyRows + totalRow + "</tbody></table>";
    }

    function toDateFilterLabel(fechaI, fechaF) {
      var desde = normalizeText(fechaI);
      var hasta = normalizeText(fechaF);
      if (!desde && !hasta) {
        return "Rango: Sin filtro";
      }
      if (desde && hasta) {
        return "Rango: " + escapeHtml(desde) + " al " + escapeHtml(hasta);
      }
      if (desde) {
        return "Desde: " + escapeHtml(desde);
      }
      return "Hasta: " + escapeHtml(hasta);
    }

    function readTopBadgeValue(prefix) {
      var normalizedPrefix = normalizeText(prefix);
      if (!normalizedPrefix) {
        return "";
      }

      var badges = Array.prototype.slice.call(document.querySelectorAll(".top-nav-right .badge"));
      for (var i = 0; i < badges.length; i += 1) {
        var text = normalizeText(badges[i].textContent);
        if (!text) {
          continue;
        }

        if (text.indexOf(normalizedPrefix) === 0) {
          return normalizeText(text.slice(normalizedPrefix.length));
        }
      }

      return "";
    }

    function resolveSupervisorDisplayName() {
      var fromConsolidado = "";
      var consolidadoRows = readConsolidadoRowsFromLoadedView();
      for (var i = 0; i < consolidadoRows.length; i += 1) {
        var candidate = normalizeText(consolidadoRows[i].nombreSupervisor);
        if (candidate) {
          fromConsolidado = candidate;
          break;
        }
      }

      var fromBadgeEmpleado = readTopBadgeValue("Empleado:");
      var fromBadgeUsuario = readTopBadgeValue("Usuario:");

      return normalizeText(fromConsolidado || fromBadgeEmpleado || fromBadgeUsuario || "-");
    }

    function getCurrentFilterSummary() {
      var empleadoInput = document.getElementById("EmpleadoFiltro");
      var fechaInicioInput = document.getElementById("FechaEntradaInicio");
      var fechaFinInput = document.getElementById("FechaEntradaFin");

      var empleado = empleadoInput ? normalizeText(empleadoInput.value) : "";
      var fechaI = fechaInicioInput ? normalizeText(fechaInicioInput.value) : "";
      var fechaF = fechaFinInput ? normalizeText(fechaFinInput.value) : "";

      return {
        supervisor: resolveSupervisorDisplayName(),
        idEmpleado: empleado,
        fechaI: fechaI,
        fechaF: fechaF
      };
    }

    function readConsolidadoRowsFromLoadedView() {
      var table = document.getElementById("supervisor-consolidado-source");
      if (!table || !table.tBodies || table.tBodies.length === 0) {
        return [];
      }

      var rows = Array.prototype.slice.call(table.tBodies[0].querySelectorAll("tr[data-empleado]"));
      return rows.map(function (row) {
        return {
          empleado: normalizeText(row.getAttribute("data-empleado")),
          totalHE: normalizeNumber(row.getAttribute("data-total-he")),
          nombreEmpleado: normalizeText(row.getAttribute("data-nombre-empleado")),
          nombreSupervisor: normalizeText(row.getAttribute("data-nombre-supervisor"))
        };
      }).filter(function (row) {
        return row.empleado.length > 0;
      });
    }

    function getExportEndpoint(handlerName) {
      var params = new URLSearchParams();
      params.set("handler", handlerName);

      var empleadoInput = document.getElementById("EmpleadoFiltro");
      var fechaInicioInput = document.getElementById("FechaEntradaInicio");
      var fechaFinInput = document.getElementById("FechaEntradaFin");
      var fechaRangoInput = document.getElementById("FechaEntradaRango") || document.getElementById("fecha-rango-filtro");

      var empleado = empleadoInput ? normalizeText(empleadoInput.value) : "";
      var fechaI = fechaInicioInput ? normalizeText(fechaInicioInput.value) : "";
      var fechaF = fechaFinInput ? normalizeText(fechaFinInput.value) : "";
      var fechaRango = fechaRangoInput ? normalizeText(fechaRangoInput.value) : "";

      if (empleado) {
        params.set("empleadoFiltro", empleado);
      }
      if (fechaI) {
        params.set("fechaEntradaInicio", fechaI);
      }
      if (fechaF) {
        params.set("fechaEntradaFin", fechaF);
      }
      if (fechaRango) {
        params.set("fechaEntradaRango", fechaRango);
      }

      return window.location.pathname + "?" + params.toString();
    }

    async function requestExportData(handlerName) {
      var endpoint = getExportEndpoint(handlerName);
      var response = null;
      try {
        response = await fetch(endpoint, {
          method: "GET",
          cache: "no-store",
          credentials: "same-origin",
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
      } catch (_error) {
        payload = null;
      }

      if (maybeRedirectForConnectivity(response, payload, null)) {
        throw new Error(CONNECTIVITY_REDIRECT_MARKER);
      }

      if (!response.ok) {
        var serverMessage = payload && payload.mensaje ? payload.mensaje : "No se pudo preparar la exportacion.";
        throw new Error(serverMessage);
      }

      if (!payload || Number(payload.codigo) !== 0) {
        var message = payload && payload.mensaje ? payload.mensaje : "Respuesta invalida al preparar exportacion.";
        throw new Error(message);
      }

      return payload;
    }

    function buildWorkbook(title, subtitleLines, tableHtml) {
      var subtitleMarkup = (subtitleLines || [])
        .filter(function (line) { return normalizeText(line).length > 0; })
        .map(function (line) { return "<div class='meta-line'>" + line + "</div>"; })
        .join("");

      return (
        "<html><head><meta charset='utf-8' />" +
        "<style>" +
        "body{font-family:Calibri,Arial,sans-serif;margin:24px;color:#243026;}" +
        ".title{font-size:22px;font-weight:700;color:#184f42;margin:0 0 6px 0;}" +
        ".meta-line{font-size:12px;color:#4d6156;margin:0 0 2px 0;}" +
        "table{border-collapse:collapse;width:100%;font-size:12px;margin-top:12px;}" +
        "th,td{border:1px solid #cad8cd;padding:6px 8px;text-align:left;}" +
        "th{background:#dfeee6;color:#153f34;font-weight:700;}" +
        ".row-alt td{background:#f7fbf8;}" +
        ".num{text-align:right;}" +
        ".center{text-align:center;}" +
        ".subtotal td{background:#fff3cf;font-weight:700;color:#6a4f0a;}" +
        ".total td{background:#d6ecdf;font-weight:700;color:#154132;}" +
        "</style></head><body>" +
        "<h1 class='title'>" + escapeHtml(title) + "</h1>" +
        subtitleMarkup +
        tableHtml +
        "</body></html>"
      );
    }

    function downloadWorkbookAsXls(filename, htmlContent) {
      var safeFileName = normalizeText(filename) || "Reporte";
      var blob = new Blob(["\ufeff", htmlContent], { type: "application/vnd.ms-excel;charset=utf-8;" });
      var url = URL.createObjectURL(blob);

      var anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = safeFileName + ".xls";
      document.body.appendChild(anchor);
      anchor.click();
      document.body.removeChild(anchor);
      URL.revokeObjectURL(url);
    }

    function buildConsolidadoTable(rows) {
      var sorted = (rows || []).slice().sort(function (left, right) {
        var empleadoLeft = normalizeText(left && left.empleado);
        var empleadoRight = normalizeText(right && right.empleado);
        return empleadoLeft.localeCompare(empleadoRight, "es", { sensitivity: "base", numeric: true });
      });

      var total = 0;
      var bodyRows = sorted.map(function (row, index) {
        var hExtras = normalizeNumber(row && row.totalHE);
        total += hExtras;

        var cls = index % 2 === 1 ? " class='row-alt'" : "";
        return (
          "<tr" + cls + ">" +
          "<td>" + escapeHtml(row && row.empleado) + "</td>" +
          "<td class='num'>" + escapeHtml(formatNumber(hExtras, 2)) + "</td>" +
          "<td>" + escapeHtml(row && row.nombreEmpleado) + "</td>" +
          "<td>" + escapeHtml(row && row.nombreSupervisor) + "</td>" +
          "</tr>"
        );
      }).join("");

      var totalRow = (
        "<tr class='total'>" +
        "<td colspan='1'>TOTAL GENERAL</td>" +
        "<td class='num'>" + escapeHtml(formatNumber(total, 2)) + "</td>" +
        "<td colspan='2'></td>" +
        "</tr>"
      );

      return (
        "<table>" +
        "<thead><tr><th>Codigo</th><th>H. Extras</th><th>Nombre</th><th>Supervisor</th></tr></thead>" +
        "<tbody>" + bodyRows + totalRow + "</tbody>" +
        "</table>"
      );
    }

    function buildMarcadasTable(rows) {
      var sorted = (rows || []).slice().sort(function (left, right) {
        var empleadoLeft = normalizeText(left && left.empleado);
        var empleadoRight = normalizeText(right && right.empleado);
        var empleadoCompare = empleadoLeft.localeCompare(empleadoRight, "es", { sensitivity: "base", numeric: true });
        if (empleadoCompare !== 0) {
          return empleadoCompare;
        }

        var dateLeft = parseDateKey(left && left.fecha, left && left.entrada);
        var dateRight = parseDateKey(right && right.fecha, right && right.entrada);
        return dateLeft - dateRight;
      });

      var htmlRows = [];
      var currentEmpleado = "";
      var subtotalAprobadas = 0;
      var totalGeneralAprobadas = 0;
      var zebraIndex = 0;

      function pushSubtotalRow() {
        if (!currentEmpleado) {
          return;
        }

        htmlRows.push(
          "<tr class='subtotal'>" +
          "<td colspan='9'>Subtotal empleado " + escapeHtml(currentEmpleado) + "</td>" +
          "<td class='num'>" + escapeHtml(formatNumber(subtotalAprobadas, 2)) + "</td>" +
          "<td></td>" +
          "</tr>"
        );
      }

      sorted.forEach(function (row) {
        var empleado = normalizeText(row && row.empleado);
        if (empleado !== currentEmpleado) {
          pushSubtotalRow();
          currentEmpleado = empleado;
          subtotalAprobadas = 0;
        }

        var aprobadas = normalizeNumber(row && row.aprobadas);
        subtotalAprobadas += aprobadas;
        totalGeneralAprobadas += aprobadas;

        var rowClass = zebraIndex % 2 === 1 ? " class='row-alt'" : "";
        zebraIndex += 1;

        htmlRows.push(
          "<tr" + rowClass + ">" +
          "<td>" + escapeHtml(row && row.empleado) + "</td>" +
          "<td>" + escapeHtml(row && row.nombres) + "</td>" +
          "<td>" + escapeHtml(row && row.apellidos) + "</td>" +
          "<td>" + escapeHtml(row && row.ubicadoEn) + "</td>" +
          "<td>" + escapeHtml(row && row.marcaEn) + "</td>" +
          "<td>" + escapeHtml(row && row.fecha) + "</td>" +
          "<td>" + escapeHtml(row && row.entrada) + "</td>" +
          "<td>" + escapeHtml(row && row.salida) + "</td>" +
          "<td class='num'>" + escapeHtml(formatNumber(row && row.laboradas, 2)) + "</td>" +
          "<td class='num'>" + escapeHtml(formatNumber(aprobadas, 2)) + "</td>" +
          "<td>" + escapeHtml(row && row.observaciones) + "</td>" +
          "</tr>"
        );
      });

      pushSubtotalRow();

      htmlRows.push(
        "<tr class='total'>" +
        "<td colspan='9'>TOTAL GENERAL APROBADAS</td>" +
        "<td class='num'>" + escapeHtml(formatNumber(totalGeneralAprobadas, 2)) + "</td>" +
        "<td></td>" +
        "</tr>"
      );

      return (
        "<table>" +
        "<thead><tr>" +
        "<th>Empleado</th><th>Nombres</th><th>Apellidos</th><th>Ubicado en</th><th>Marca en</th>" +
        "<th>Fecha</th><th>Entrada</th><th>Salida</th><th>Laboradas</th><th>Aprobadas</th><th>Observaciones</th>" +
        "</tr></thead>" +
        "<tbody>" + htmlRows.join("") + "</tbody>" +
        "</table>"
      );
    }

    async function runConsolidadoExport() {
      disableExportButtons(true);
      try {
        setStatus("1/3 Preparando consolidado...", "export-status-loading");
        var filtros = getCurrentFilterSummary();
        var data = readConsolidadoRowsFromLoadedView();
        if (data.length) {
          var supervisorNombre = normalizeText(data[0] && data[0].nombreSupervisor);
          if (supervisorNombre) {
            filtros.supervisor = supervisorNombre;
          }
        }

        if (!data.length) {
          setStatus("1/3 Consultando consolidado...", "export-status-loading");
          var payload = await requestExportData("ExportConsolidadoData");
          var filtrosApi = payload.filtros || {};
          data = Array.isArray(payload.datos) ? payload.datos : [];
          filtros = {
            supervisor: normalizeText(filtrosApi.supervisor) || filtros.supervisor,
            idEmpleado: normalizeText(filtrosApi.idEmpleado) || filtros.idEmpleado,
            fechaI: normalizeText(filtrosApi.fechaI) || filtros.fechaI,
            fechaF: normalizeText(filtrosApi.fechaF) || filtros.fechaF
          };
        }

        if (!data.length) {
          throw new Error("No hay datos para exportar en consolidado.");
        }

        setStatus("2/3 Dando formato al archivo XLS...", "export-status-loading");
        var subtitle = [
          "Supervisor: " + escapeHtml(filtros.supervisor || "-"),
          "Empleado: " + escapeHtml(filtros.idEmpleado || "Todos"),
          toDateFilterLabel(filtros.fechaI, filtros.fechaF),
          "Generado: " + escapeHtml(new Date().toLocaleString("es-ES"))
        ];
        var table = buildConsolidadoTable(data);
        var workbook = buildWorkbook("Consolidado de Horas Extras", subtitle, table);

        setStatus("3/3 Descargando archivo...", "export-status-loading");
        downloadWorkbookAsXls("ConsolidadoHE", workbook);
        setStatus("Exportacion de consolidado completada.", "export-status-ok", 6000);
      } catch (error) {
        if (error && error.message === CONNECTIVITY_REDIRECT_MARKER) {
          return;
        }

        var message = error && error.message ? error.message : "No se pudo exportar consolidado.";
        setStatus(message, "export-status-error", 7000);
      } finally {
        disableExportButtons(false);
      }
    }

    async function runMarcadasExport() {
      disableExportButtons(true);
      try {
        setStatus("1/3 Consultando reporte de marcadas...", "export-status-loading");
        var payload = await requestExportData("ExportMarcadasData");
        var data = Array.isArray(payload.datos) ? payload.datos : [];
        if (!data.length) {
          throw new Error("No hay datos para exportar en reporte de marcadas.");
        }

        setStatus("2/3 Ordenando por empleado y calculando subtotales...", "export-status-loading");
        var filtros = payload.filtros || {};
        var subtitle = [
          "Supervisor: " + escapeHtml(filtros.supervisor || "-"),
          "Empleado: " + escapeHtml(filtros.idEmpleado || "Todos"),
          toDateFilterLabel(filtros.fechaI, filtros.fechaF),
          "Generado: " + escapeHtml(new Date().toLocaleString("es-ES"))
        ];
        var table = buildMarcadasTable(data);
        var workbook = buildWorkbook("Reporte de Marcadas", subtitle, table);

        setStatus("3/3 Descargando archivo...", "export-status-loading");
        downloadWorkbookAsXls("MarcadasGeneral", workbook);
        setStatus("Exportacion de marcadas completada.", "export-status-ok", 6000);
      } catch (error) {
        if (error && error.message === CONNECTIVITY_REDIRECT_MARKER) {
          return;
        }

        var message = error && error.message ? error.message : "No se pudo exportar reporte de marcadas.";
        setStatus(message, "export-status-error", 7000);
      } finally {
        disableExportButtons(false);
      }
    }

    async function runConsolidadoPivotExport() {
      disableExportButtons(true);
      try {
        setStatus("1/3 Leyendo vista cargada...", "export-status-loading");
        var rows = readLoadedTableRows();
        if (!rows.length) {
          throw new Error("No hay datos en la vista para exportar consolidado pivotado.");
        }

        var range = resolveEffectiveDateRange();
        var monthBuckets = buildMonthBuckets(range.start, range.end);
        if (!monthBuckets.length) {
          throw new Error("No se pudo resolver el rango de meses para el consolidado pivotado.");
        }

        setStatus("2/3 Pivotando horas extras por mes...", "export-status-loading");
        var filtros = getCurrentFilterSummary();
        filtros.fechaI = formatAsMonthDayYear(range.start);
        filtros.fechaF = formatAsMonthDayYear(range.end);

        var subtitle = [
          "Supervisor: " + escapeHtml(filtros.supervisor || "-"),
          "Empleado: " + escapeHtml(filtros.idEmpleado || "Todos"),
          toDateFilterLabel(filtros.fechaI, filtros.fechaF),
          "Generado: " + escapeHtml(new Date().toLocaleString("es-ES"))
        ];

        var pivotTable = buildPivotConsolidadoTable(rows, monthBuckets, range);
        if (!pivotTable) {
          throw new Error("No hay datos para exportar consolidado pivotado en el rango seleccionado.");
        }
        var workbook = buildWorkbook("Consolidado Pivotado de Horas Extras", subtitle, pivotTable);

        setStatus("3/3 Descargando archivo...", "export-status-loading");
        downloadWorkbookAsXls("ConsolidadoPivotadoHE", workbook);
        setStatus("Exportacion de consolidado pivotado completada.", "export-status-ok", 6000);
      } catch (error) {
        if (error && error.message === CONNECTIVITY_REDIRECT_MARKER) {
          return;
        }

        var message = error && error.message ? error.message : "No se pudo exportar consolidado pivotado.";
        setStatus(message, "export-status-error", 7000);
      } finally {
        disableExportButtons(false);
      }
    }

    if (btnConsolidado) {
      btnConsolidado.addEventListener("click", function () {
        void runConsolidadoExport();
      });
    }

    if (btnMarcadas) {
      btnMarcadas.addEventListener("click", function () {
        void runMarcadasExport();
      });
    }

    if (btnConsolidadoPivot) {
      btnConsolidadoPivot.addEventListener("click", function () {
        void runConsolidadoPivotExport();
      });
    }
  }

  function initializeHorasExtrasFrontend() {
    attachOfflineConnectivityWatcher();
    attachSupervisorDateRangePicker();
    attachSupervisorAutoSave();
    attachSupervisorTableSorting();
    attachSupervisorExports();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initializeHorasExtrasFrontend);
  } else {
    initializeHorasExtrasFrontend();
  }
})();
