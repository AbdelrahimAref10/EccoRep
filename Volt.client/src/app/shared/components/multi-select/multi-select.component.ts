import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  forwardRef,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe } from '../../pipes/translate.pipe';

export interface MultiSelectOption {
  value: string | number | boolean;
  label: string;
  description?: string;
}

@Component({
  selector: 'app-multi-select',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './multi-select.component.html',
  styleUrl: './multi-select.component.css',
  host: {
    '[class.ms-host--open]': 'open'
  },
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MultiSelectComponent),
      multi: true
    }
  ]
})
export class MultiSelectComponent implements ControlValueAccessor {
  private readonly host = inject(ElementRef<HTMLElement>);

  @Input() label = '';
  @Input() placeholder = '';
  @Input() options: MultiSelectOption[] = [];
  @Input() multiple = true;
  @Input() searchable = true;
  @Input() searchPlaceholder = '';
  @Input() emptyText = '';
  @Input() clearable = true;
  @Input() disabled = false;
  @Input() layout: 'list' | 'table' = 'list';
  @Input() primaryColumnLabel = '';
  @Input() secondaryColumnLabel = '';

  @Input()
  set values(value: Array<string | number | boolean> | null | undefined) {
    this.internalValues = value ? [...value] : [];
  }
  get values(): Array<string | number | boolean> {
    return this.internalValues;
  }

  @Output() valuesChange = new EventEmitter<Array<string | number | boolean>>();

  open = false;
  query = '';
  internalValues: Array<string | number | boolean> = [];

  private onChange: (value: unknown) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  get selectedCount(): number {
    return this.internalValues.length;
  }

  get triggerLabel(): string {
    if (!this.selectedCount) {
      return this.placeholder;
    }
    if (!this.multiple || this.selectedCount === 1) {
      const selected = this.internalValues[0];
      const match = this.options.find(o => this.sameValue(o.value, selected));
      return match?.label || this.placeholder;
    }
    return `${this.selectedCount}`;
  }

  get triggerDescription(): string {
    if (!this.selectedCount || (this.multiple && this.selectedCount > 1)) {
      return '';
    }
    const selected = this.internalValues[0];
    const match = this.options.find(o => this.sameValue(o.value, selected));
    const description = (match?.description || '').trim();
    return !description || description === '—' ? '' : description;
  }

  get isTable(): boolean {
    return this.layout === 'table';
  }

  get filteredOptions(): MultiSelectOption[] {
    const q = this.query.trim().toLowerCase();
    if (!q) {
      return this.options;
    }
    return this.options.filter(o =>
      o.label.toLowerCase().includes(q) || (o.description || '').toLowerCase().includes(q)
    );
  }

  writeValue(value: unknown): void {
    if (this.multiple) {
      this.internalValues = Array.isArray(value) ? [...value] : value != null && value !== '' ? [value as string | number | boolean] : [];
      return;
    }
    this.internalValues = value != null && value !== '' ? [value as string | number | boolean] : [];
  }

  registerOnChange(fn: (value: unknown) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  toggleOpen(event?: Event): void {
    event?.stopPropagation();
    if (this.disabled) {
      return;
    }
    this.open = !this.open;
    if (!this.open) {
      this.query = '';
      this.onTouched();
    }
  }

  isSelected(value: string | number | boolean): boolean {
    return this.internalValues.some(v => this.sameValue(v, value));
  }

  toggleValue(value: string | number | boolean, event?: Event): void {
    event?.stopPropagation();
    if (this.disabled) {
      return;
    }

    if (!this.multiple) {
      this.commitValues([value]);
      this.open = false;
      this.query = '';
      this.onTouched();
      return;
    }

    const next = this.isSelected(value)
      ? this.internalValues.filter(v => !this.sameValue(v, value))
      : [...this.internalValues, value];
    this.commitValues(next);
  }

  clear(event?: Event): void {
    event?.stopPropagation();
    if (this.disabled || !this.clearable) {
      return;
    }
    this.commitValues([]);
    this.onTouched();
  }

  selectAll(event?: Event): void {
    event?.stopPropagation();
    if (this.disabled || !this.multiple) {
      return;
    }
    this.commitValues(this.options.map(o => o.value));
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      if (this.open) {
        this.onTouched();
      }
      this.open = false;
      this.query = '';
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open) {
      this.onTouched();
    }
    this.open = false;
    this.query = '';
  }

  private commitValues(next: Array<string | number | boolean>): void {
    this.internalValues = [...next];
    this.valuesChange.emit([...next]);
    this.onChange(this.multiple ? [...next] : (next[0] ?? null));
  }

  private sameValue(a: string | number | boolean, b: string | number | boolean): boolean {
    return String(a) === String(b);
  }
}
