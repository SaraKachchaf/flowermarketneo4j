import Swal from 'sweetalert2';

const professionalAlerts = {
    /**
     * Show a professional success message
     * @param {string} title 
     * @param {string} text 
     */
    success: (title, text) => {
        return Swal.fire({
            icon: 'success',
            title: title || 'Succès !',
            text: text || '',
            confirmButtonColor: '#2d5a27', // Matching the site's green theme
            timer: 3000,
            timerProgressBar: true,
        });
    },

    /**
     * Show a professional error message
     * @param {string} title 
     * @param {string} text 
     */
    error: (title, text) => {
        return Swal.fire({
            icon: 'error',
            title: title || 'Erreur !',
            text: text || 'Une erreur est survenue.',
            confirmButtonColor: '#e74c3c',
        });
    },

    /**
     * Show a professional confirmation dialog
     * @param {string} title 
     * @param {string} text 
     * @param {string} confirmText 
     * @returns {Promise<boolean>}
     */
    confirm: async (title, text, confirmText = 'Oui, confirmer') => {
        const result = await Swal.fire({
            title: title || 'Êtes-vous sûr ?',
            text: text || '',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#2d5a27',
            cancelButtonColor: '#95a5a6',
            confirmButtonText: confirmText,
            cancelButtonText: 'Annuler'
        });
        return result.isConfirmed;
    }
};

export default professionalAlerts;
