import type {FaceDto} from "@/entities/photo/model.ts";
import {Popover, PopoverContent, PopoverTrigger} from "@/components/ui/popover.tsx";
import {Label} from "@/components/ui/label.tsx";

import './FaceOverlay.css';
import {getGenderText} from "@photobank/shared";

export const FaceOverlay = ({
                                face,
                                index,
                                style,
                            }: {
    face: FaceDto;
    index: number;
    style?: React.CSSProperties;
}) => (
    <Popover key={face.id || index}>
        <PopoverTrigger asChild>
            <div
                style={style}
                className="absolute hover:border-blue-400 hover:bg-blue-200/20 face-box"
            >
                <span className="absolute top-0 right-0 text-xs bg-blue-500 text-white px-1 rounded shadow z-20">
                    {index + 1}
                </span>
            </div>
        </PopoverTrigger>
        <PopoverContent className="w-80 bg-popover border-border shadow-xl">
            <div className="space-y-3">
                <h4 className="font-semibold border-b border-border pb-2">Face Details</h4>
                <div className="grid grid-cols-2 gap-3 text-sm">
                    <div>
                        <Label className="text-muted-foreground">Age</Label>
                        <p className="font-medium">{face.age || "Unknown"}</p>
                    </div>
                    <div>
                        <Label className="text-muted-foreground">Gender</Label>
                        <p className="font-medium">{getGenderText(face.gender)}</p>
                    </div>
                    {face.personId && (
                        <div className="col-span-2">
                            <Label className="text-muted-foreground">Person ID</Label>
                            <p className="font-medium">{face.personId}</p>
                        </div>
                    )}
                </div>
                {face.friendlyFaceAttributes && (
                    <div>
                        <Label className="text-muted-foreground">Attributes</Label>
                        <p className="text-sm mt-1">{face.friendlyFaceAttributes}</p>
                    </div>
                )}
            </div>
        </PopoverContent>
    </Popover>
);
