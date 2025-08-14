import { Label } from "@/shared/ui/label";

interface ScoreBarProps {
    label: string;
    score?: number;
    colorClass: string;
}

export const ScoreBar = ({ label, score, colorClass }: ScoreBarProps) => {
    const percent = ((score ?? 0) * 100).toFixed(1);

    return (
        <div>
            <Label className="text-muted-foreground text-xs">{label}</Label>
            <div className="flex items-center gap-2 mt-1">
                <div className="flex-1 bg-muted rounded-full h-2">
                    <div
                        className={`${colorClass} h-2 rounded-full transition-all duration-500`}
                        style={{ width: `${percent}%` }}
                    />
                </div>
                <span className="text-sm font-medium">{percent}%</span>
            </div>
        </div>
    );
};
