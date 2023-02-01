let mymap;
let navigatorLat, navigatorLong;

const greenIcon = new L.Icon({
    iconUrl: '../img/marker-icon-2x-green.png',
    shadowUrl: '../img/marker-shadow.png',
    iconSize: [25, 41],
    iconAnchor: [12, 41],
    popupAnchor: [1, -34],
    shadowSize: [41, 41]
});

const busStopIcon = L.icon({
    iconUrl: '../img/bus-stop.png',
    iconSize: [44, 44], // size of the icon

});

const vehicleIcon = L.icon({
    iconUrl: '../img/bus.png',
    iconSize: [44, 44], // size of the icon

});

const Stadia_AlidadeSmoothDark =
    L.tileLayer('https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a>, &copy; <a href="https://openmaptiles.org/">OpenMapTiles</a> &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors'
    });

const Stadia_Outdoors =
    L.tileLayer('https://tiles.stadiamaps.com/tiles/outdoors/{z}/{x}/{y}{r}.png', {
        maxZoom: 20,
        attribution: '&copy; <a href="https://stadiamaps.com/">Stadia Maps</a>, &copy; <a href="https://openmaptiles.org/">OpenMapTiles</a> &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors'
    });

const baseMaps =
    {
        "Light Mode": Stadia_Outdoors,
        "Dark Mode": Stadia_AlidadeSmoothDark
    };

let busTrackingMarker = L.marker([], { icon: vehicleIcon })


mymap = L.map('map', {
    layers: [Stadia_AlidadeSmoothDark, Stadia_Outdoors],
    zoomControl: false
})



window.onload = () => {
    'use strict';

    if ('serviceWorker' in navigator) {
        navigator.serviceWorker
            .register('./sw.js');
    }

    mymap.setView([51.505, -0.09], 13);
}

$(document).ready(() => {
    $('#useLocation').on('click', useLocationClick);
    $('#stopQuery').on('keyup', findStopQuery);
});

function stopClick(e) {

    let line_name, direction_name, departure_time, departure_time_diff, departure_time_mins, data, departure_string, expected_departure_time, expected_departure_time_diff;

    $.ajax({
        url: `${siteURI}/stopData?atcocode=${this.options.ATCOCode}`,
        type: 'GET',
        success: result => {

            $('#stop_common_name').text(this.options.CommonName);
            $('#stop_details').text(this.options.StopDetails)
            $('#stop_info_container').html('');

            data = JSON.parse(result);

            if (!data.Siri.ServiceDelivery.StopMonitoringDelivery.MonitoredStopVisit) {

                $('#stop_info_container').html(`<div class="text-center text-muted mb-3">
                    <h3><span class="material-icons">info</span></h3>
                    <h4>No services expected</h4>
                </div>`);

            } else {
                data.Siri.ServiceDelivery.StopMonitoringDelivery.MonitoredStopVisit.forEach((item) => {
                    console.log(item)
                    direction_name = item.MonitoredVehicleJourney.DirectionName;
                    line_name = item.MonitoredVehicleJourney.PublishedLineName;
                    departure_time = new Date(item.MonitoredVehicleJourney.MonitoredCall.AimedDepartureTime);
                    expected_departure_time = item.MonitoredVehicleJourney.MonitoredCall.ExpectedDepartureTime ? new Date(item.MonitoredVehicleJourney.MonitoredCall.ExpectedDepartureTime) : null;
                    operator_ref = item.MonitoredVehicleJourney.OperatorRef.replace('_noc_', '');

                    departure_time_diff = Math.abs(new Date(Date.now()) - new Date(departure_time));
                    departure_time_mins = Math.floor((departure_time_diff / 1000) / 60);

                    expected_departure_time ? expected_departure_time_diff = Math.abs(new Date(Date.now()) - new Date(expected_departure_time)) : null;
                    expected_departure_time ? expected_departure_time_mins = Math.floor((expected_departure_time_diff / 1000) / 60): null;

                    if (expected_departure_time) {
                        expected_departure_time_mins !== departure_time_mins ? departure_string = departure_time_mins : departure_string = `<strike>${departure_time_mins}</strike> ${expected_departure_time_mins}`;
                    } else {
                        departure_string = departure_time_mins;
                    }
                    
                    const rowTemplate = `<div class="row border-bottom border-light mb-3 pb-3 stop_info_vehicle" data-operatorRef="${operator_ref}" data-lineRef="${line_name}">
                                        <div class="col-8">
                                            <h4><span class="material-icons">directions_bus</span> <span class="badge rounded-pill text-bg-warning">${line_name}</span></h4>
                                            <span>${direction_name}</span>
                                        </div>
                                        <div class="col-4 text-end">
                                            <h5 class="mb-0">${departure_string} <small class="text-muted">Mins</small></h5>
                                        </div>
                                    </div>`

                    $('#stop_info_container').append(rowTemplate);
                });
            }

            $('.stop_info_vehicle').on('click', stopVehicleClick)

            const height = window.innerHeight;
            const vhPixels = height * 0.5
            window.scrollBy(0, 0);
            window.scrollBy(0, vhPixels);

        }
    });

}

function findStopQuery(e) {
    if ($(this).val().length > 3) { 
        $.ajax({
            url: `${siteURI}/stopSearch?query=${$(this).val()}`,
            type: 'GET',
            success: result => {

                $('#autocomplete').children('.list-group').html('');

                JSON.parse(result).forEach((item) => {
                    let stopDetails = []
                    item.Street ? stopDetails.push(item.Street) : null;
                    item.LocalityName ? stopDetails.push(item.LocalityName) : null;
                    item.ParentLocalityName ? stopDetails.push(item.ParentLocalityName) : null;
                    stopDetails.join();

                    $('#autocomplete').children('.list-group').append(`<li data-latitude="${item.Latitude}" data-longitude="${item.Longitude}" data-commonname="${item.CommonName}" class="list-group-item">${item.CommonName}, ${stopDetails}</li>`)
                });
                $('#autocomplete').show();
                $('#autocomplete').children('.list-group').children('.list-group-item').on('click', stopQuerySelect)

            }
        });
    }
}


function stopVehicleClick(e) {
    $.ajax({
        url: `${siteURI}/vehicleTrackingData?operatorRef=${$(this).data('operatorref')}&lineRef=${$(this).data('lineref')}`,
        type: 'GET',
        success: result => {
           const data = JSON.parse(result);

            data.Siri.ServiceDelivery.VehicleMonitoringDelivery.VehicleActivity.forEach((item) => {
                const lat = item.MonitoredVehicleJourney.VehicleLocation.Latitude;
                const lng = item.MonitoredVehicleJourney.VehicleLocation.Longitude;
                busTrackingMarker.setLatLng([lat, lng]).addTo(mymap);
                mymap.setView([lat, lng], 17);
            });
        }
    });
}

function useLocationClick() {
    if ('geolocation' in navigator) {

        $('#stopQuery').val('');
        $('#stop_common_name').text('');
        $('#stop_details').text('')
        $('#stop_info_container').html('');

        navigator.geolocation.getCurrentPosition((position) => {
            navigaotrLat = position.coords.latitude;
            navigatorLong = position.coords.longitude;
            mymap.setView([navigaotrLat, navigatorLong], 13);
            let marker = L.marker([position.coords.latitude, position.coords.longitude], { icon: greenIcon }).addTo(mymap);
            mymap.setZoom(17);

            $.ajax({
                url: `${siteURI}/stops?lat=${navigaotrLat}&lng=${navigatorLong}`,
                type: 'GET',
                success: result => {
                    JSON.parse(result).forEach((item) => {
                        let stopDetails = []
                        item.Street ? stopDetails.push(item.Street) : null;
                        item.LocalityName ? stopDetails.push(item.LocalityName) : null;
                        item.ParentLocalityName ? stopDetails.push(item.ParentLocalityName) : null;
                        stopDetails.join();

                        let marker = L.marker([item.Latitude, item.Longitude], { icon: busStopIcon, ATCOCode: item.ATCOCode, CommonName: item.CommonName, StopDetails: stopDetails }).addTo(mymap).on('click', stopClick);
                    });
                }
            });

        });

    } else {
        mymap.setView([51.505, -0.09], 13);
    }

}

function stopQuerySelect() {

    $('#stopQuery').val($(this).data('commonname'));
    $('#autocomplete').hide();
    $('#stop_common_name').text('');
    $('#stop_details').text('')
    $('#stop_info_container').html('');

    mymap.setView([$(this).data('latitude'), $(this).data('longitude')], 13);
    mymap.setZoom(17);

    $.ajax({
        url: `${siteURI}/stops?lat=${$(this).data('latitude')}&lng=${$(this).data('longitude')}`,
        type: 'GET',
        success: result => {
            JSON.parse(result).forEach((item) => {
                let stopDetails = []
                item.Street ? stopDetails.push(item.Street) : null;
                item.LocalityName ? stopDetails.push(item.LocalityName) : null;
                item.ParentLocalityName ? stopDetails.push(item.ParentLocalityName) : null;
                stopDetails.join();

                let marker = L.marker([item.Latitude, item.Longitude], { icon: busStopIcon, ATCOCode: item.ATCOCode, CommonName: item.CommonName, StopDetails: stopDetails }).addTo(mymap).on('click', stopClick);
            });
        }
    });

}