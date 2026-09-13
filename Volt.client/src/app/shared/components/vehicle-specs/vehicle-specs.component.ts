import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-vehicle-specs',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './vehicle-specs.component.html',
  styleUrls: ['./vehicle-specs.component.css']
})
export class VehicleSpecsComponent {
  @Input() color: string | null | undefined;
  @Input() type: string | null | undefined;
  @Input() model: string | null | undefined;
  @Input() price: number | null | undefined;
  @Input() speedKmh: number | null | undefined;
  @Input() engineCapacityCc: number | null | undefined;
  @Input() compact = false;
  @Input() showPrice = true;

  get hasFacts(): boolean {
    return !!(
      this.color ||
      this.type ||
      this.model ||
      this.speedKmh != null ||
      this.engineCapacityCc != null
    );
  }

  get colorSwatch(): string | null {
    const raw = (this.color || '').trim().toLowerCase();
    if (!raw) return null;
    const named: Record<string, string> = {
      black: '#111827',
      white: '#f8fafc',
      red: '#ef4444',
      blue: '#2563eb',
      green: '#16a34a',
      yellow: '#eab308',
      orange: '#f97316',
      silver: '#94a3b8',
      grey: '#6b7280',
      gray: '#6b7280',
      gold: '#d4a017',
      brown: '#92400e',
      beige: '#d6c4a8',
      navy: '#1e3a8a',
      purple: '#7c3aed',
      pink: '#ec4899',
      teal: '#14b8a6'
    };
    if (named[raw]) return named[raw];
    if (/^#([0-9a-f]{3}|[0-9a-f]{6})$/i.test(raw)) return raw;
    return null;
  }
}
