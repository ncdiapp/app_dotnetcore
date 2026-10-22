import React, { useEffect, useState } from 'react';
import { fetchAuthenticatedImageBlobUrl } from '../../../webapi/fileEndpoints';
import { toAppResourcePath, unwrapQuoted } from './imageValue';

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
    const kind = kindProp ?? (/^https?:\/\//i.test(clean) ? 'external' : 'resource');
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

    const box = size === 'card'
        ? 'relative h-44 w-full'
        : size === 'compact'
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
