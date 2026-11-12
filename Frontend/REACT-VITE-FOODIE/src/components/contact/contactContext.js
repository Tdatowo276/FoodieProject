import { createContext, useContext } from 'react';

export const ContactContext = createContext(null);

/** Mở các hộp liên hệ từ bất kỳ đâu: const { openMap, openPhone, openFeedback } = useContact(); */
export const useContact = () => useContext(ContactContext);
