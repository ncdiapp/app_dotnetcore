import React, { useEffect, useMemo, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';

type GoogleMapViewLayoutProps = {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onSelectionChanged?: (selectedItems: any[]) => void;
};

const GOOGLE_MAP_SCRIPT_ID = 'appai-google-map-script';
const GOOGLE_MAP_API_KEY = 'AIzaSyATxSPycLHs-FGANWtexJoIRQcXaRq08f0';

function loadGoogleMapsApi(): Promise<void> {
  const win = window as any;
  if (win?.google?.maps?.Map) return Promise.resolve();
  const existing = document.getElementById(GOOGLE_MAP_SCRIPT_ID) as HTMLScriptElement | null;
  if (existing) {
    return new Promise((resolve, reject) => {
      existing.addEventListener('load', () => resolve(), { once: true });
      existing.addEventListener('error', () => reject(new Error('Failed to load Google Maps API')), { once: true });
    });
  }
  return new Promise((resolve, reject) => {
    const s = document.createElement('script');
    s.id = GOOGLE_MAP_SCRIPT_ID;
    s.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(GOOGLE_MAP_API_KEY)}`;
    s.async = true;
    s.defer = true;
    s.onload = () => resolve();
    s.onerror = () => reject(new Error('Failed to load Google Maps API'));
    document.head.appendChild(s);
  });
}

function getCellValue(dict: Record<string, any>, field: any): any {
  if (!dict || !field) return null;
  const id = field?.Id;
  const path = field?.SysTableFiledPath;
  return (
    dict[id] ??
    dict[String(id ?? '')] ??
    (path ? dict[path] : undefined) ??
    (path ? dict[String(path)] : undefined) ??
    null
  );
}

function parsePosition(raw: any): { lat: number; lng: number } | null {
  if (!raw) return null;
  let data: any = raw;
  if (typeof raw === 'string') {
    try {
      data = JSON.parse(raw);
    } catch {
      return null;
    }
  }
  const lat = Number(data?.lat ?? data?.Lat ?? data?.latitude ?? data?.Latitude);
  const lng = Number(data?.lng ?? data?.Lng ?? data?.longitude ?? data?.Longitude);
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;
  return { lat, lng };
}

export const GoogleMapViewLayout: React.FC<GoogleMapViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onSelectionChanged,
}) => {
  const { theme } = useTheme();
  const emInternalCodeRegistrationForGoogleMapView = useEnumValues('EmInternalCodeRegistrationForGoogleMapView');
  const mapContainerRef = useRef<HTMLDivElement | null>(null);

  const mapMarkerPositionCode = useMemo(() => {
    const obj = emInternalCodeRegistrationForGoogleMapView || {};
    const entries = Object.entries(obj);
    const hit =
      entries.find(([k]) => k === 'MapMarkerPositionObject') ||
      entries.find(([k]) => String(k).toLowerCase().includes('position'));
    return Number(hit?.[1] ?? NaN);
  }, [emInternalCodeRegistrationForGoogleMapView]);

  const mapMarkerLabelCode = useMemo(() => {
    const obj = emInternalCodeRegistrationForGoogleMapView || {};
    const entries = Object.entries(obj);
    const hit =
      entries.find(([k]) => k === 'MapMarkerLabelText') ||
      entries.find(([k]) => String(k).toLowerCase().includes('label'));
    return Number(hit?.[1] ?? NaN);
  }, [emInternalCodeRegistrationForGoogleMapView]);

  const columns = useMemo(() => {
    const list = viewDto?.Columns ?? viewDto?.AppSearchViewFieldList ?? [];
    return Array.isArray(list) ? list : [];
  }, [viewDto?.Columns, viewDto?.AppSearchViewFieldList]);

  const markerPositionField = useMemo(
    () => columns.find((c: any) => Number(c?.EmInternalCodeRegistration) === mapMarkerPositionCode),
    [columns, mapMarkerPositionCode]
  );

  const markerLabelField = useMemo(
    () => columns.find((c: any) => Number(c?.EmInternalCodeRegistration) === mapMarkerLabelCode),
    [columns, mapMarkerLabelCode]
  );

  const markerItems = useMemo(() => {
    const rows = Array.isArray(viewDataList) ? viewDataList : [];
    return rows
      .map((row: any) => {
        const dict = row?.DictViewColumnIDKeyValue ?? {};
        const pos = parsePosition(getCellValue(dict, markerPositionField));
        if (!pos) return null;
        const label = String(getCellValue(dict, markerLabelField) ?? '').trim();
        return { pos, label, row };
      })
      .filter(Boolean) as Array<{ pos: { lat: number; lng: number }; label: string; row: any }>;
  }, [viewDataList, markerPositionField, markerLabelField]);

  useEffect(() => {
    let cancelled = false;
    let map: any = null;
    let markers: any[] = [];

    const renderMap = async () => {
      if (!mapContainerRef.current) return;
      try {
        await loadGoogleMapsApi();
      } catch {
        return;
      }
      if (cancelled || !mapContainerRef.current) return;
      const g = (window as any)?.google;
      if (!g?.maps?.Map) return;

      const fallbackCenter = {
        lat: Number(dataModel?.searchDto?.CurrentMapPositionLat ?? dataModel?.searchResultDto?.PositionLat ?? 45.5017),
        lng: Number(dataModel?.searchDto?.CurrentMapPositionLng ?? dataModel?.searchResultDto?.PositionLng ?? -73.78115),
      };
      const center = markerItems[0]?.pos ?? fallbackCenter;

      map = new g.maps.Map(mapContainerRef.current, {
        center,
        zoom: markerItems.length > 0 ? 9 : 5,
        mapTypeControl: false,
      });

      const bounds = new g.maps.LatLngBounds();
      markerItems.forEach((item) => {
        const marker = new g.maps.Marker({
          position: item.pos,
          map,
          title: item.label || '',
          icon: 'https://maps.google.com/mapfiles/kml/shapes/library_maps.png',
        });
        markers.push(marker);
        bounds.extend(item.pos);
        if (item.label) {
          const info = new g.maps.InfoWindow({ content: item.label, disableAutoPan: true });
          info.open(map, marker);
          marker.addListener('click', () => {
            info.setContent(`You have selected: ${item.label}`);
            info.open(map, marker);
            onSelectionChanged?.([item.row]);
          });
        } else {
          marker.addListener('click', () => onSelectionChanged?.([item.row]));
        }
      });

      if (markerItems.length > 1) {
        map.fitBounds(bounds);
      }
    };

    void renderMap();
    return () => {
      cancelled = true;
      markers.forEach((m) => m?.setMap?.(null));
      markers = [];
      map = null;
    };
  }, [markerItems, dataModel?.searchDto?.CurrentMapPositionLat, dataModel?.searchDto?.CurrentMapPositionLng, dataModel?.searchResultDto?.PositionLat, dataModel?.searchResultDto?.PositionLng, onSelectionChanged]);

  if (!markerPositionField) {
    return (
      <div className={`w-full h-full flex items-center justify-center text-xs ${theme.label}`}>
        Configure a field with "MapMarkerPositionObject" in "Internal Code Registration".
      </div>
    );
  }

  return <div ref={mapContainerRef} className="w-full h-full bg-gray-100" />;
};

export default GoogleMapViewLayout;
