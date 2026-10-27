import React, { useEffect, useState } from 'react';
import { fetchAuthenticatedImageBlobUrl } from '../../../webapi/fileEndpoints';
import { toAppResourcePath, unwrapQuoted } from './imageValue';

const APP_API_IMAGE = /\/api\/(?:resources|files)\//i;

interface Props {
    url: string;
    alt?: string;
    size?: 'cell' | 'compact' | 'card';
    /** When set from grid: 'resource' uses session fetch; 'external' uses URL as-is. */
    kind?: 'resource' | 'external' | null;
}

/** Renders an image URL. App resource URLs are loaded with the session header. */
export const DataRenderImage: React.FC<Props> = ({ url, alt, size = 'cell', kind: kindProp }) => {
    const clean = unwrapQuoted(url);
    // App /api/files|resources always need session fetch — never treat as bare external img src.
    const kind = APP_API_IMAGE.test(clean) || /^\d+$/.test(clean)
        ? 'resource'
        : (kindProp ?? (/^https?:\/\//i.test(clean) ? 'external' : 'resource'));
    const [src, setSrc] = useState<string | null>(kind === 'external' ? clean : null);

    useEffect(() => {
        if (kind !== 'resource') {
            setSrc(kind === 'external' ? clean : null);
            return;
        }
        let cancelled = false;
        setSrc(null);
        fetchAuthenticatedImageBlobUrl(toAppResourcePath(clean)).then(blobUrl => {
            if (!cancelled) setSrc(blobUrl);
        });
        return () => { cancelled = true; };
    }, [clean, kind]);

    if (size === 'card') {
        if (!src) return <div className="absolute inset-0" style={{ background: 'rgba(0,0,0,0.04)' }} />;
        return (
            <img
                src={src}
                alt={alt || ''}
                className="absolute inset-0 w-full h-full object-cover object-center"
                onError={(e) => { e.currentTarget.style.display = 'none'; }}
            />
        );
    }

    const box = size === 'compact'
        ? 'relative h-32 w-full'
        : 'relative h-[60px] w-[60px]';

    if (!src) {
        return <div className={box} style={{ background: 'rgba(0,0,0,0.04)' }} />;
    }

    return (
        <div className={box} style={{ background: 'rgba(0,0,0,0.04)' }}>
            <img
                src={src}
                alt={alt || ''}
                className="absolute inset-0 m-auto max-h-full max-w-full object-contain"
                onError={(e) => { e.currentTarget.style.display = 'none'; }}
            />
        </div>
    );
};
