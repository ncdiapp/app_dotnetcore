import React, { useEffect, useState } from 'react';
import { fetchAuthenticatedImageBlobUrl } from '../../../webapi/fileEndpoints';
import { imageUrlKind, toAppResourcePath } from './imageValue';

interface Props {
    url: string;
    alt?: string;
    size?: 'cell' | 'compact' | 'card';
}

/** Renders an image URL. App resource URLs are loaded with the session header. */
export const DataRenderImage: React.FC<Props> = ({ url, alt, size = 'cell' }) => {
    const kind = imageUrlKind(url);
    const [src, setSrc] = useState<string | null>(kind === 'external' ? url : null);

    useEffect(() => {
        if (kind !== 'resource') {
            setSrc(kind === 'external' ? url : null);
            return;
        }
        let cancelled = false;
        setSrc(null);
        fetchAuthenticatedImageBlobUrl(toAppResourcePath(url)).then(blobUrl => {
            if (!cancelled) setSrc(blobUrl);
        });
        return () => { cancelled = true; };
    }, [url, kind]);

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
