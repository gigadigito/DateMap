var dateMap = dateMap || {};

dateMap._maps = {};
dateMap._dotNetRefs = {};

dateMap.initMap = function (containerId, dotNetRef) {
    var container = document.getElementById(containerId);
    if (!container) return;
    if (dateMap._maps[containerId]) {
        dateMap._maps[containerId].setTarget(null);
        dateMap._maps[containerId] = null;
    }

    var vectorSource = new ol.source.Vector();

    var vectorLayer = new ol.layer.Vector({
        source: vectorSource,
        style: function (feature) {
            var color = feature.get('categoryColor') || '#3388ff';
            return new ol.style.Style({
                image: new ol.style.Circle({
                    radius: 10,
                    fill: new ol.style.Fill({ color: color }),
                    stroke: new ol.style.Stroke({ color: '#fff', width: 2 })
                })
            });
        }
    });

    var popup = document.createElement('div');
    popup.className = 'ol-popup';
    popup.style.cssText = 'position:absolute;background:white;padding:10px;border-radius:4px;box-shadow:0 2px 6px rgba(0,0,0,.3);pointer-events:auto;min-width:180px;z-index:1;';
    popup.style.display = 'none';

    var closer = document.createElement('a');
    closer.className = 'ol-popup-closer';
    closer.href = '#';
    closer.textContent = '\u00d7';
    closer.style.cssText = 'position:absolute;top:2px;right:6px;text-decoration:none;font-size:18px;color:#333;cursor:pointer;';
    closer.onclick = function (e) { e.preventDefault(); popup.style.display = 'none'; overlay.setPosition(undefined); return false; };
    popup.appendChild(closer);

    var content = document.createElement('div');
    content.className = 'ol-popup-content';
    popup.appendChild(content);

    var overlay = new ol.Overlay({ element: popup, autoPan: true, autoPanAnimation: { duration: 250 } });

    var map = new ol.Map({
        target: container,
        layers: [
            new ol.layer.Tile({ source: new ol.source.OSM() }),
            vectorLayer
        ],
        overlays: [overlay],
        view: new ol.View({ center: ol.proj.fromLonLat([0, 0]), zoom: 2 })
    });

    map.on('click', function (evt) {
        var feature = map.forEachFeatureAtPixel(evt.pixel, function (f) { return f; });
        if (feature) {
            var coords = feature.getGeometry().getCoordinates();
            overlay.setPosition(coords);
            var title = feature.get('title') || '';
            var eventDate = feature.get('eventDate') || '';
            var placeName = feature.get('placeName') || '';
            var categoryName = feature.get('categoryName') || '';
            var categoryColor = feature.get('categoryColor') || '';

            var html = '';
            if (title) { var h = document.createElement('h6'); h.textContent = title; h.style.margin = '0 0 4px 0'; html += h.outerHTML; }
            if (eventDate) { var d = document.createElement('div'); d.textContent = eventDate; d.style.cssText = 'color:#666;font-size:13px;margin-bottom:2px;'; html += d.outerHTML; }
            if (placeName) { var p = document.createElement('div'); p.textContent = placeName; p.style.cssText = 'color:#666;font-size:13px;margin-bottom:2px;'; html += p.outerHTML; }
            if (categoryName) { var c = document.createElement('div'); c.style.cssText = 'font-size:13px;'; if (categoryColor) { var sw = document.createElement('span'); sw.style.cssText = 'display:inline-block;width:12px;height:12px;border-radius:50%;background:' + categoryColor + ';margin-right:4px;vertical-align:middle;'; c.appendChild(sw); } c.appendChild(document.createTextNode(categoryName)); html += c.outerHTML; }

            content.innerHTML = html;

            vectorSource.getFeatures().forEach(function (f) { f.setStyle(null); });
            feature.setStyle(new ol.style.Style({
                image: new ol.style.Circle({
                    radius: 12,
                    fill: new ol.style.Fill({ color: feature.get('categoryColor') || '#3388ff' }),
                    stroke: new ol.style.Stroke({ color: '#000', width: 3 })
                })
            }));

            if (dotNetRef) {
                var eventId = feature.get('eventId');
                dotNetRef.invokeMethodAsync('OnMarkerSelected', eventId);
            }
        } else {
            popup.style.display = 'none';
            overlay.setPosition(undefined);
            vectorSource.getFeatures().forEach(function (f) { f.setStyle(null); });
        }
    });

    dateMap._maps[containerId] = map;
    dateMap._dotNetRefs[containerId] = dotNetRef;
    return map;
};

dateMap.updateMarkers = function (containerId, eventsJson) {
    var map = dateMap._maps[containerId];
    if (!map) return;

    var layers = map.getLayers().getArray();
    var vectorLayer = layers.find(function (l) { return l.getSource && l.getSource() instanceof ol.source.Vector; });
    if (!vectorLayer) return;
    var source = vectorLayer.getSource();
    source.clear();

    var events = JSON.parse(eventsJson);
    if (!events || events.length === 0) return;

    events.forEach(function (ev) {
        var coord = ol.proj.fromLonLat([ev.longitude, ev.latitude]);
        var feature = new ol.Feature({
            geometry: new ol.geom.Point(coord),
            eventId: ev.id,
            title: ev.title,
            eventDate: ev.eventDate,
            placeName: ev.placeName,
            categoryName: ev.categoryName,
            categoryColor: ev.categoryColor || '#3388ff'
        });
        source.addFeature(feature);
    });

    if (events.length === 1) {
        var coord = ol.proj.fromLonLat([events[0].longitude, events[0].latitude]);
        map.getView().setCenter(coord);
        map.getView().setZoom(15);
    } else {
        var extent = source.getExtent();
        map.getView().fit(extent, { padding: [50, 50, 50, 50], maxZoom: 16 });
    }
};

dateMap.flyToEvent = function (containerId, longitude, latitude) {
    var map = dateMap._maps[containerId];
    if (!map) return;
    var coord = ol.proj.fromLonLat([longitude, latitude]);
    map.getView().setCenter(coord);
    map.getView().setZoom(16);

    var layers = map.getLayers().getArray();
    var vectorLayer = layers.find(function (l) { return l.getSource && l.getSource() instanceof ol.source.Vector; });
    if (vectorLayer) {
        var source = vectorLayer.getSource();
        source.getFeatures().forEach(function (f) { f.setStyle(null); });
        var target = source.getFeatures().find(function (f) {
            var c = f.getGeometry().getCoordinates();
            return Math.abs(c[0] - coord[0]) < 0.001 && Math.abs(c[1] - coord[1]) < 0.001;
        });
        if (target) {
            target.setStyle(new ol.style.Style({
                image: new ol.style.Circle({
                    radius: 12,
                    fill: new ol.style.Fill({ color: target.get('categoryColor') || '#3388ff' }),
                    stroke: new ol.style.Stroke({ color: '#000', width: 3 })
                })
            }));
        }
    }
};

dateMap.updateSize = function (containerId) {
    var map = dateMap._maps[containerId];
    if (map) map.updateSize();
};

dateMap.destroyMap = function (containerId) {
    if (dateMap._maps[containerId]) {
        dateMap._maps[containerId].setTarget(null);
        dateMap._maps[containerId] = null;
    }
    delete dateMap._dotNetRefs[containerId];
};

dateMap.initLocationPicker = function (containerId, dotNetRef) {
    var container = document.getElementById(containerId);
    if (!container) return;
    if (dateMap._maps[containerId]) {
        dateMap._maps[containerId].setTarget(null);
        dateMap._maps[containerId] = null;
    }

    var vectorSource = new ol.source.Vector();
    var vectorLayer = new ol.layer.Vector({
        source: vectorSource,
        style: new ol.style.Style({
            image: new ol.style.Circle({
                radius: 10,
                fill: new ol.style.Fill({ color: '#e74c3c' }),
                stroke: new ol.style.Stroke({ color: '#fff', width: 2 })
            })
        })
    });

    var map = new ol.Map({
        target: container,
        layers: [
            new ol.layer.Tile({ source: new ol.source.OSM() }),
            vectorLayer
        ],
        view: new ol.View({ center: ol.proj.fromLonLat([-46.63, -23.55]), zoom: 12 })
    });

    map.on('click', function (evt) {
        var lonlat = ol.proj.toLonLat(evt.coordinate);
        var longitude = lonlat[0];
        var latitude = lonlat[1];
        vectorSource.clear();
        var feature = new ol.Feature({ geometry: new ol.geom.Point(evt.coordinate) });
        vectorSource.addFeature(feature);
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnLocationSelected', latitude, longitude);
        }
    });

    dateMap._maps[containerId] = map;
};

dateMap.setPickerLocation = function (containerId, longitude, latitude) {
    var map = dateMap._maps[containerId];
    if (!map) return;
    var layers = map.getLayers().getArray();
    var vectorLayer = layers.find(function (l) { return l.getSource && l.getSource() instanceof ol.source.Vector; });
    if (vectorLayer) {
        var source = vectorLayer.getSource();
        source.clear();
        if (longitude !== null && latitude !== null) {
            var coord = ol.proj.fromLonLat([longitude, latitude]);
            source.addFeature(new ol.Feature({ geometry: new ol.geom.Point(coord) }));
            map.getView().setCenter(coord);
            map.getView().setZoom(15);
        }
    }
};

dateMap.clearPicker = function (containerId) {
    var map = dateMap._maps[containerId];
    if (!map) return;
    var layers = map.getLayers().getArray();
    var vectorLayer = layers.find(function (l) { return l.getSource && l.getSource() instanceof ol.source.Vector; });
    if (vectorLayer) vectorLayer.getSource().clear();
};
