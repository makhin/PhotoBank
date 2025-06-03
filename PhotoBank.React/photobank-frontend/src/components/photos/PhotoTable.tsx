import { useMemo } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { useAppSelector } from '@/app/hooks';
import type { ICellRendererParams, ColDef } from 'ag-grid-community';

interface Column {
    field: string;
    label: string;
}

interface Props {
    allColumns: Column[];
    visibleColumns: string[];
}

function getCellRenderer(field: string) {
    if (field === 'thumbnail') {
        return (params: ICellRendererParams) =>
            params.value ? (
                <img
                    src={`data:image/jpeg;base64,${params.value}`}
                    alt="preview"
                    style={{ width: 80, height: 80 }}
                />
            ) : null;
    }
    if (field === 'tags' || field === 'persons') {
        return (params: ICellRendererParams) =>
            (params.value || []).map((v: { name: string }, i: number) => (
                <span key={i} className="bg-muted text-xs px-2 py-1 rounded mr-1">
          {v.name}
        </span>
            ));
    }
    return undefined;
}

export default function PhotoTable({ allColumns, visibleColumns }: Props) {
    const photos = useAppSelector(state => state.photos.items);

    const columnDefs: ColDef[] = useMemo(() => {
        return allColumns
            .filter(col => visibleColumns.includes(col.field))
            .map<ColDef>(col => ({
                headerName: col.label,
                field: col.field,
                cellRenderer: getCellRenderer(col.field),
                sortable: true,
                filter: true,
                resizable: true,
                ...(col.field === 'thumbnail' ? { width: 100 } : {}),
            }));
    }, [allColumns, visibleColumns]);

    return (
        <div className="ag-theme-alpine" style={{ height: 500 }}>
            <AgGridReact rowData={photos} columnDefs={columnDefs} domLayout="autoHeight" pagination={false} />
        </div>
    );
}
