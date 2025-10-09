export { DEFAULT_PHOTO_FILTER } from './filter/defaults';

export const apiErrorMsg = 'API error:';
export const botTokenNotDefinedError = 'BOT_TOKEN is not defined';
export const apiCredentialsNotDefinedError =
  'API_EMAIL or API_PASSWORD is not defined';
export const uploadStorageName = 'Telegram';

// frontend shared constants
export const METADATA_CACHE_KEY = 'photobank_metadata_cache';
export const METADATA_CACHE_VERSION = 1;

export const PHOTO_FILTER_STORAGE_VERSION = 1;
export const PHOTO_FILTER_STORAGE_KEY = `photobank_photo_filter_v${PHOTO_FILTER_STORAGE_VERSION}`;

export const MAX_VISIBLE_PERSONS_LG = 3;
export const MAX_VISIBLE_TAGS_LG = 3;

export const MAX_VISIBLE_PERSONS_SM = 2;
export const MAX_VISIBLE_TAGS_SM = 2;

export const DEFAULT_FORM_VALUES = {
  caption: undefined,
  storages: [] as string[],
  paths: [] as string[],
  personNames: [] as string[],
  tagNames: [] as string[],
  isBW: undefined,
  isAdultContent: undefined,
  isRacyContent: undefined,
  thisDay: undefined,
  dateFrom: undefined as Date | null | undefined,
  dateTo: undefined as Date | null | undefined,
} as const;

// NavBar labels
export const navbarFilterLabel = 'Filter';
export const navbarPhotosLabel = 'Photos';
export const navbarProfileLabel = 'Profile';
export const navbarLoginLabel = 'Login';
export const navbarLogoutLabel = 'Logout';
export const navbarRegisterLabel = 'Register';
export const navbarUsersLabel = 'Users';

// PhotoPreviewModal
export const previewModalFallbackTitle = 'Preview';
export const loadingText = 'Loading...';

// Filter form labels
export const captionLabel = 'Caption';
export const captionPlaceholder = 'Enter caption...';
export const dateFromLabel = 'Date From';
export const selectDatePlaceholder = 'Select date';
export const dateToLabel = 'Date To';
export const storagesLabel = 'Storages';
export const selectStoragesPlaceholder = 'Select storages';
export const pathsLabel = 'Paths';
export const selectPathsPlaceholder = 'Select paths';
export const personsLabel = 'Persons';
export const selectPersonsPlaceholder = 'Select persons';
export const tagsLabel = 'Tags';
export const selectTagsPlaceholder = 'Select tags';
export const blackWhiteLabel = 'Black-White';
export const adultContentLabel = 'Adult Content';
export const racyContentLabel = 'Racy Content';
export const thisDayLabel = 'This Day';

// Face overlay
export const faceDetailsTitle = 'Face Details';
export const ageLabel = 'Age';
export const unknownLabel = 'Unknown';
export const genderLabel = 'Gender';
export const personIdLabel = 'Person ID';
export const attributesLabel = 'Attributes';

// Face person selector
export const unassignedLabel = 'Unassigned';
export const facePrefix = 'Face';
export const searchPersonPlaceholder = 'Search person...';
export const noPersonFoundText = 'No person found.';
export const noneLabel = 'None';

// Status card
export const botRunningText = 'Bot is running';

// Login page
export const loginTitle = 'Login';
export const invalidCredentialsMsg = 'Invalid email or password';
export const emailLabel = 'Email';
export const passwordLabel = 'Password';
export const stayLoggedInLabel = 'Stay logged in';
export const loginButtonText = 'Login';

// Register page
export const registerTitle = 'Register';
export const registerButtonText = 'Register';

// Logout page
export const loggingOutMsg = 'Logging out...';

// Filter page
export const filterFormTitle = 'Advanced Filter Form';
export const applyFiltersButton = 'Apply Filters';

// Photo list page
export const photoGalleryTitle = 'Photo Gallery';
export const filterButtonText = 'Filter';
export const loadMoreButton = 'Load More';

// Photo details page
export const photoPropertiesTitle = 'Photo Properties';
export const nameLabel = 'Name';
export const idLabel = 'ID';
export const takenDateLabel = 'Taken Date';
export const widthLabel = 'Width';
export const heightLabel = 'Height';
export const scaleLabel = 'Scale';
export const orientationLabel = 'Orientation';
export const locationLabel = 'Location';
export const openInMapsText = 'Open in Google Maps';
export const tagsTitle = 'Tags';
export const captionsTitle = 'Captions';
export const contentAnalysisTitle = 'Content Analysis';
export const adultScoreLabel = 'Adult Score';
export const racyScoreLabel = 'Racy Score';
export const detectedFacesTitle = 'Detected Faces';
export const showFaceBoxesLabel = 'Show face boxes';
export const hoverFaceHint =
  'Hover over the blue boxes on the image to see face details.';

// Profile page
export const myProfileTitle = 'My Profile';
export const emailPrefix = 'Email:';
export const phoneNumberLabel = 'Phone number';
export const telegramLabel = 'Telegram';
export const saveButtonText = 'Save';
export const rolesTitle = 'Roles';
export const logoutButtonText = 'Logout';

// Photo list table headers
export const colIdLabel = 'ID';
export const colPreviewLabel = 'Preview';
export const colNameLabel = 'Name';
export const colDateLabel = 'Date';
export const colStorageLabel = 'Storage';
export const colFlagsLabel = 'Flags';
export const colDetailsLabel = 'Details';

// Service page
export const serviceInfoTitle = 'Technical Information';

// OpenAI page
export const openAiPageTitle = 'OpenAI Chat';
export const openAiSendButton = 'Send';
export const openAiPromptPlaceholder = 'Enter your request...';
export const navbarOpenAiLabel = 'OpenAI';
