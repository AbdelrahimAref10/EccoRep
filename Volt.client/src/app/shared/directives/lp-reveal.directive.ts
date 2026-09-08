import {
  AfterViewInit,
  Directive,
  ElementRef,
  Input,
  OnDestroy,
  inject
} from '@angular/core';

@Directive({
  selector: '[lpReveal]',
  standalone: true
})
export class LpRevealDirective implements AfterViewInit, OnDestroy {
  private readonly el = inject(ElementRef<HTMLElement>);
  private observer?: IntersectionObserver;

  /** Extra delay in ms before adding the in-view class (stagger). */
  @Input() lpRevealDelay = 0;

  /** Once visible, keep revealed (default true). */
  @Input() lpRevealOnce = true;

  ngAfterViewInit(): void {
    const node = this.el.nativeElement;
    node.classList.add('lp-reveal');

    if (typeof window === 'undefined' || !('IntersectionObserver' in window)) {
      node.classList.add('is-inview');
      return;
    }

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      node.classList.add('is-inview');
      return;
    }

    this.observer = new IntersectionObserver(
      entries => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            window.setTimeout(() => node.classList.add('is-inview'), this.lpRevealDelay);
            if (this.lpRevealOnce) {
              this.observer?.unobserve(node);
            }
          } else if (!this.lpRevealOnce) {
            node.classList.remove('is-inview');
          }
        }
      },
      { threshold: 0.16, rootMargin: '0px 0px -8% 0px' }
    );

    this.observer.observe(node);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
