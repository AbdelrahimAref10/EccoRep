export type LandingVehicleSlug = 'golf' | 'medical' | 'scooter';

export interface LandingVehicleSpec {
  labelKey: string;
  valueKey: string;
}

export interface LandingVehicle {
  slug: LandingVehicleSlug;
  tone: 'golf' | 'medical' | 'scooter';
  cover: string;
  gallery: string[];
  tagKey: string;
  titleKey: string;
  subtitleKey: string;
  leadKey: string;
  storyKeys: string[];
  highlightKeys: { titleKey: string; descKey: string }[];
  idealKeys: string[];
  specs: LandingVehicleSpec[];
}

const BASE = 'assets/Vehicles';

export const LANDING_VEHICLES: LandingVehicle[] = [
  {
    slug: 'golf',
    tone: 'golf',
    cover: `${BASE}/Golf.jpg`,
    gallery: [`${BASE}/Golf2.jpg`, `${BASE}/Golf.jpg`],
    tagKey: 'landing.scooters.items.golf.tag',
    titleKey: 'landing.scooters.items.golf.title',
    subtitleKey: 'landing.vehicleDetail.golf.subtitle',
    leadKey: 'landing.vehicleDetail.golf.lead',
    storyKeys: [
      'landing.vehicleDetail.golf.story1',
      'landing.vehicleDetail.golf.story2',
      'landing.vehicleDetail.golf.story3'
    ],
    highlightKeys: [
      { titleKey: 'landing.vehicleDetail.golf.h1t', descKey: 'landing.vehicleDetail.golf.h1d' },
      { titleKey: 'landing.vehicleDetail.golf.h2t', descKey: 'landing.vehicleDetail.golf.h2d' },
      { titleKey: 'landing.vehicleDetail.golf.h3t', descKey: 'landing.vehicleDetail.golf.h3d' },
      { titleKey: 'landing.vehicleDetail.golf.h4t', descKey: 'landing.vehicleDetail.golf.h4d' }
    ],
    idealKeys: [
      'landing.vehicleDetail.golf.ideal1',
      'landing.vehicleDetail.golf.ideal2',
      'landing.vehicleDetail.golf.ideal3'
    ],
    specs: [
      { labelKey: 'landing.vehicleDetail.specs.seats', valueKey: 'landing.vehicleDetail.golf.specs.seats' },
      { labelKey: 'landing.vehicleDetail.specs.range', valueKey: 'landing.vehicleDetail.golf.specs.range' },
      { labelKey: 'landing.vehicleDetail.specs.speed', valueKey: 'landing.vehicleDetail.golf.specs.speed' },
      { labelKey: 'landing.vehicleDetail.specs.charge', valueKey: 'landing.vehicleDetail.golf.specs.charge' }
    ]
  },
  {
    slug: 'medical',
    tone: 'medical',
    cover: `${BASE}/MedicalChair.jpg`,
    gallery: [`${BASE}/MedicalChair.jpg`, `${BASE}/chair2.jpg`],
    tagKey: 'landing.scooters.items.medical.tag',
    titleKey: 'landing.scooters.items.medical.title',
    subtitleKey: 'landing.vehicleDetail.medical.subtitle',
    leadKey: 'landing.vehicleDetail.medical.lead',
    storyKeys: [
      'landing.vehicleDetail.medical.story1',
      'landing.vehicleDetail.medical.story2',
      'landing.vehicleDetail.medical.story3'
    ],
    highlightKeys: [
      { titleKey: 'landing.vehicleDetail.medical.h1t', descKey: 'landing.vehicleDetail.medical.h1d' },
      { titleKey: 'landing.vehicleDetail.medical.h2t', descKey: 'landing.vehicleDetail.medical.h2d' },
      { titleKey: 'landing.vehicleDetail.medical.h3t', descKey: 'landing.vehicleDetail.medical.h3d' },
      { titleKey: 'landing.vehicleDetail.medical.h4t', descKey: 'landing.vehicleDetail.medical.h4d' }
    ],
    idealKeys: [
      'landing.vehicleDetail.medical.ideal1',
      'landing.vehicleDetail.medical.ideal2',
      'landing.vehicleDetail.medical.ideal3'
    ],
    specs: [
      { labelKey: 'landing.vehicleDetail.specs.weight', valueKey: 'landing.vehicleDetail.medical.specs.weight' },
      { labelKey: 'landing.vehicleDetail.specs.fold', valueKey: 'landing.vehicleDetail.medical.specs.fold' },
      { labelKey: 'landing.vehicleDetail.specs.wheels', valueKey: 'landing.vehicleDetail.medical.specs.wheels' },
      { labelKey: 'landing.vehicleDetail.specs.support', valueKey: 'landing.vehicleDetail.medical.specs.support' }
    ]
  },
  {
    slug: 'scooter',
    tone: 'scooter',
    cover: `${BASE}/Scooter.jpg`,
    gallery: [`${BASE}/Scooter.jpg`, `${BASE}/scooter2.jpg`],
    tagKey: 'landing.scooters.items.scooter.tag',
    titleKey: 'landing.scooters.items.scooter.title',
    subtitleKey: 'landing.vehicleDetail.scooter.subtitle',
    leadKey: 'landing.vehicleDetail.scooter.lead',
    storyKeys: [
      'landing.vehicleDetail.scooter.story1',
      'landing.vehicleDetail.scooter.story2',
      'landing.vehicleDetail.scooter.story3'
    ],
    highlightKeys: [
      { titleKey: 'landing.vehicleDetail.scooter.h1t', descKey: 'landing.vehicleDetail.scooter.h1d' },
      { titleKey: 'landing.vehicleDetail.scooter.h2t', descKey: 'landing.vehicleDetail.scooter.h2d' },
      { titleKey: 'landing.vehicleDetail.scooter.h3t', descKey: 'landing.vehicleDetail.scooter.h3d' },
      { titleKey: 'landing.vehicleDetail.scooter.h4t', descKey: 'landing.vehicleDetail.scooter.h4d' }
    ],
    idealKeys: [
      'landing.vehicleDetail.scooter.ideal1',
      'landing.vehicleDetail.scooter.ideal2',
      'landing.vehicleDetail.scooter.ideal3'
    ],
    specs: [
      { labelKey: 'landing.vehicleDetail.specs.range', valueKey: 'landing.vehicleDetail.scooter.specs.range' },
      { labelKey: 'landing.vehicleDetail.specs.speed', valueKey: 'landing.vehicleDetail.scooter.specs.speed' },
      { labelKey: 'landing.vehicleDetail.specs.weight', valueKey: 'landing.vehicleDetail.scooter.specs.weight' },
      { labelKey: 'landing.vehicleDetail.specs.charge', valueKey: 'landing.vehicleDetail.scooter.specs.charge' }
    ]
  }
];

export function getLandingVehicle(slug: string): LandingVehicle | undefined {
  return LANDING_VEHICLES.find(v => v.slug === slug);
}
