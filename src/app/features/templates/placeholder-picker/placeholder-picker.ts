import { Component, EventEmitter, Input, Output } from '@angular/core';

import { Placeholder } from '../template.models';

interface PlaceholderTooltip {
  placeholderId: number;
  text: string;
  left: number;
  top: number;
}

@Component({
  selector: 'app-placeholder-picker',
  standalone: true,
  imports: [],
  templateUrl: './placeholder-picker.html',
  styleUrl: './placeholder-picker.scss',
})
export class PlaceholderPicker {
  @Input({ required: true }) placeholders!: Placeholder[];
  @Input({ required: true }) activePlaceholderCount!: number;

  @Output() readonly placeholderSelected = new EventEmitter<Placeholder>();

  activeTooltip?: PlaceholderTooltip;

  showPlaceholderTooltip(event: MouseEvent | FocusEvent, placeholder: Placeholder): void {
    const description = placeholder.displayName.trim();
    if (!description) {
      return;
    }

    const target = event.currentTarget as HTMLElement;
    const bounds = target.getBoundingClientRect();
    const tooltipHalfWidth = 140;
    const viewportMargin = 16;
    const centeredLeft = bounds.left + bounds.width / 2;
    const left = Math.min(
      Math.max(centeredLeft, viewportMargin + tooltipHalfWidth),
      window.innerWidth - viewportMargin - tooltipHalfWidth,
    );

    this.activeTooltip = {
      placeholderId: placeholder.placeholderId,
      text: description,
      left,
      top: bounds.top - 10,
    };
  }

  hidePlaceholderTooltip(): void {
    this.activeTooltip = undefined;
  }
}
