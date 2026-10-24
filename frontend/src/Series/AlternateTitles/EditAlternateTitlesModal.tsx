import React, { useCallback, useRef } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { fetchSeries } from 'Store/Actions/seriesActions';
import EditAlternateTitlesModalContent from './EditAlternateTitlesModalContent';

interface EditAlternateTitlesModalProps {
  isOpen: boolean;
  seriesId: number;
  seriesTitle: string;
  onModalClose(): void;
}

function EditAlternateTitlesModal({
  isOpen,
  seriesId,
  seriesTitle,
  onModalClose,
}: EditAlternateTitlesModalProps) {
  const dispatch = useDispatch();
  const hasChanges = useRef(false);

  const handleAlternateTitlesChange = useCallback(() => {
    hasChanges.current = true;
  }, []);

  const handleModalClose = useCallback(() => {
    if (hasChanges.current) {
      hasChanges.current = false;
      dispatch(fetchSeries({ id: seriesId }));
    }

    onModalClose();
  }, [seriesId, dispatch, onModalClose]);

  return (
    <Modal isOpen={isOpen} size={sizes.LARGE} onModalClose={handleModalClose}>
      <EditAlternateTitlesModalContent
        seriesId={seriesId}
        seriesTitle={seriesTitle}
        onAlternateTitlesChange={handleAlternateTitlesChange}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default EditAlternateTitlesModal;
