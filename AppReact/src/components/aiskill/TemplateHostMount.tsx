import React, { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

/**
 * Renders TemplateContent control LAYOUT HTML and portals children into #hostId
 * (e.g. chartHost / gridHost) for React Wijmo.
 */
const TemplateHostMount: React.FC<{
    html: string;
    hostId: string;
    className?: string;
    children: React.ReactNode;
}> = ({ html, hostId, className, children }) => {
    const wrapRef = useRef<HTMLDivElement>(null);
    const [hostEl, setHostEl] = useState<HTMLElement | null>(null);

    useEffect(() => {
        const root = wrapRef.current;
        if (!root || !html?.trim()) {
            setHostEl(null);
            return;
        }
        root.innerHTML = html;
        const el =
            (root.querySelector(`#${hostId}`) as HTMLElement | null)
            || (root.querySelector(`[data-host="${hostId}"]`) as HTMLElement | null);
        setHostEl(el);
    }, [html, hostId]);

    return (
        <div className={`template-host-mount min-w-0 max-w-full ${className || ''}`}>
            <div ref={wrapRef} className="min-w-0 max-w-full" />
            {hostEl
                ? createPortal(children, hostEl)
                : (!html?.trim() ? children : null)}
        </div>
    );
};

export default TemplateHostMount;
