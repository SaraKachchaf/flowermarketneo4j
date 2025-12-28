import React, { useState, useRef, useEffect } from 'react';
import './CustomDropdown.css';
import { ChevronDown, Check } from 'lucide-react';

const CustomDropdown = ({
    options,
    value,
    onChange,
    icon: Icon,
    placeholder = "Sélectionner...",
    className = ""
}) => {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);

    // Close on click outside
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const handleSelect = (optionValue) => {
        onChange(optionValue);
        setIsOpen(false);
    };

    // Find label for current value
    const selectedLabel = options.find(opt => opt.value === value)?.label || placeholder;

    return (
        <div className={`custom-dropdown-container ${className}`} ref={dropdownRef}>
            <button
                className={`dropdown-trigger ${isOpen ? 'open' : ''}`}
                onClick={() => setIsOpen(!isOpen)}
                type="button"
            >
                <div className="trigger-content">
                    {Icon && <Icon size={18} className="dropdown-icon" />}
                    <span className="selected-label">{selectedLabel}</span>
                </div>
                <ChevronDown size={14} className={`chevron-icon ${isOpen ? 'rotate' : ''}`} />
            </button>

            {isOpen && (
                <div className="dropdown-menu">
                    {options.map((option) => (
                        <div
                            key={option.value}
                            className={`dropdown-item ${value === option.value ? 'selected' : ''}`}
                            onClick={() => handleSelect(option.value)}
                        >
                            <span>{option.label}</span>
                            {value === option.value && <Check size={14} className="check-icon" />}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

export default CustomDropdown;
