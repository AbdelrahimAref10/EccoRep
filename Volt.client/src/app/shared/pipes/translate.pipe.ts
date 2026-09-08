import { Pipe, PipeTransform, inject } from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';

@Pipe({
  name: 't',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform {
  private readonly localeService = inject(LocaleService);

  transform(key: string, params?: Record<string, string | number>): string {
    // Touch locale signal so impure pipe refreshes on language change
    this.localeService.locale();
    return this.localeService.translate(key, params);
  }
}
